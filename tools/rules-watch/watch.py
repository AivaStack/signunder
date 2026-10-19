"""Watches the primary sources behind the rule packs for changes (make watch).

Every source a pack cites is fetched, reduced to its readable text (or, for PDFs,
the file's SHA-256), and compared with the snapshot saved when it was last
accepted. A change is reported, never acted on: rules change only through a pack
edit, `make rules`, Eoin's review and the release steps.

    python tools/rules-watch/watch.py check [--report report.md]   # exit 1 on any change or failure
    python tools/rules-watch/watch.py accept                        # save what is live now as the baseline

Standard library only.
"""

from __future__ import annotations

import argparse
import difflib
import glob
import hashlib
import json
import re
import subprocess
import sys
import tempfile
from dataclasses import dataclass
from html.parser import HTMLParser
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PACKS = ROOT / "src" / "Signunder.Rules" / "packs"
SNAPSHOTS = Path(__file__).resolve().parent / "snapshots"
# Sites differ in which clients they let through: some government sites refuse anything
# that does not look like curl, Sidley refuses curl, EUR-Lex challenges curl but serves
# Python's client. Each is tried in turn; every one names the watcher honestly.
CURL_AGENTS = ["curl/8.7.1 SignUnder-RulesWatch", "SignUnder-RulesWatch/1.0"]
PYTHON_AGENT = "SignUnder rules watch (+https://signunder.com/checker; hello@signunder.com)"
# Sources no script can read (they refuse every automated request): listed in each
# report as a reminder to check them by hand.
MANUAL = json.loads((Path(__file__).resolve().parent / "manual.json").read_text())
REGIONAL = {"Your California Privacy Choices", "Do Not Sell or Share My Personal Information"}
SKIP_TAGS = {"script", "style", "noscript", "svg", "head", "template", "iframe", "title"}
DIFF_LINES = 40


class _Text(HTMLParser):
    """The visible text of an HTML page, one block per line."""

    def __init__(self) -> None:
        super().__init__(convert_charrefs=True)
        self.parts: list[str] = []
        self._skip = 0

    def handle_starttag(self, tag, attrs):
        if tag in SKIP_TAGS:
            self._skip += 1
        elif tag in {"p", "div", "li", "tr", "br", "h1", "h2", "h3", "h4", "section", "article", "td", "th"}:
            self.parts.append("\n")

    def handle_endtag(self, tag):
        if tag in SKIP_TAGS and self._skip:
            self._skip -= 1

    def handle_data(self, data):
        if not self._skip:
            self.parts.append(data)


def visible_text(html: str) -> str:
    parser = _Text()
    parser.feed(html)
    lines = (re.sub(r"\s+", " ", line).strip() for line in "".join(parser.parts).split("\n"))
    # A line that is only a long number is a session or build id, not policy text; some
    # footers add links for visitors from particular places (CI runs in the US).
    return "\n".join(line for line in lines if line and not re.fullmatch(r"\d{10,}", line) and line not in REGIONAL)


@dataclass
class Source:
    url: str
    cited_by: list[str]

    @property
    def key(self) -> str:
        return hashlib.sha256(self.url.encode()).hexdigest()[:16]

    @property
    def snapshot(self) -> Path:
        return SNAPSHOTS / f"{self.key}.txt"


def sources() -> list[Source]:
    found: dict[str, list[str]] = {}
    for path in sorted(glob.glob(str(PACKS / "*.json"))):
        pack = json.loads(Path(path).read_text())
        for source in pack["sources"]:
            found.setdefault(source["url"], []).append(f"{pack['pack']}:{source['id']}")
    return [Source(url, cited) for url, cited in sorted(found.items())]


class Unreadable(Exception):
    pass


def _curl(url: str, agent: str) -> tuple[bytes, str]:
    with tempfile.NamedTemporaryFile() as body_file:
        result = subprocess.run(
            ["curl", "-sSL", "--max-time", "45", "-A", agent, "-H", "Accept-Language: en",
             "-o", body_file.name, "-w", "%{http_code} %{content_type}", url],
            capture_output=True, text=True,
        )
        if result.returncode != 0:
            raise Unreadable(result.stderr.strip() or f"curl exit {result.returncode}")
        status, _, content_type = result.stdout.partition(" ")
        if status != "200":
            raise Unreadable(f"HTTP {status}")
        return Path(body_file.name).read_bytes(), content_type


def _python(url: str) -> tuple[bytes, str]:
    import urllib.error
    import urllib.parse
    import urllib.request

    request = urllib.request.Request(
        urllib.parse.quote(url, safe=":/?&=%#"), headers={"User-Agent": PYTHON_AGENT, "Accept-Language": "en"}
    )
    try:
        with urllib.request.urlopen(request, timeout=45) as response:
            if response.status != 200:
                raise Unreadable(f"HTTP {response.status}")
            return response.read(), response.headers.get("Content-Type", "")
    except (urllib.error.URLError, TimeoutError, OSError) as error:
        raise Unreadable(str(getattr(error, "reason", error))) from error


def fetch(url: str) -> str:
    """The source as comparable text: visible text for HTML, a digest for anything else."""
    attempts = [lambda agent=agent: _curl(url, agent) for agent in CURL_AGENTS] + [lambda: _python(url)]
    reasons = []
    for attempt in attempts:
        try:
            body, content_type = attempt()
            break
        except Unreadable as error:
            reasons.append(str(error))
    else:
        raise Unreadable("; ".join(dict.fromkeys(reasons)))
    kind = content_type.split(";")[0].strip().lower()
    charset = re.search(r"charset=([\w-]+)", content_type)
    if kind in {"text/html", "application/xhtml+xml"}:
        text = visible_text(body.decode(charset.group(1) if charset else "utf-8", errors="replace"))
        if len(text) < 200:
            # A page built in the browser gives a script nothing to compare; saying
            # "unchanged" about it would be false.
            raise Unreadable("no readable text: the page is built by JavaScript")
        return text
    return f"{kind} sha256:{hashlib.sha256(body).hexdigest()} bytes:{len(body)}"


def saved(source: Source) -> str | None:
    if not source.snapshot.exists():
        return None
    return source.snapshot.read_text().split("\n", 1)[1]


def save(source: Source, text: str) -> None:
    SNAPSHOTS.mkdir(exist_ok=True)
    source.snapshot.write_text(f"{source.url}\n{text}\n")


def check(report_path: str | None) -> int:
    changed, failed, new, manual, same = [], [], [], [], 0
    for source in sources():
        if source.url in MANUAL:
            manual.append(source)
            continue
        try:
            live = fetch(source.url)
        except Unreadable as error:
            failed.append((source, str(error)))
            continue
        before = saved(source)
        if before is None:
            new.append(source)
        elif before.rstrip("\n") != live.rstrip("\n"):
            diff = list(difflib.unified_diff(before.splitlines(), live.splitlines(), "saved", "live", n=1, lineterm=""))
            changed.append((source, diff))
        else:
            same += 1

    lines = [
        f"Checked {len(sources())} sources: {same} unchanged, {len(changed)} changed, {len(failed)} unreadable, "
        f"{len(new)} without a baseline, {len(manual)} to check by hand.",
        "",
    ]
    for source, diff in changed:
        lines += [f"### Changed: {source.url}", f"Cited by {', '.join(source.cited_by)}.", "", "```diff"]
        lines += diff[:DIFF_LINES] + ([f"… {len(diff) - DIFF_LINES} more lines"] if len(diff) > DIFF_LINES else [])
        lines += ["```", ""]
    for source, reason in failed:
        lines += [f"### Unreadable: {source.url}", f"Cited by {', '.join(source.cited_by)}. {reason}", ""]
    for source in new:
        lines += [f"### No baseline: {source.url}", f"Cited by {', '.join(source.cited_by)}.", ""]
    for source in manual:
        lines += [f"### Check by hand: {source.url}", f"Cited by {', '.join(source.cited_by)}. {MANUAL[source.url]}", ""]
    if changed or failed or new:
        lines += [
            "Review each change against the rules that cite it. If a rule must change: edit the pack, `make rules`, "
            "republish the review page for Eoin, then follow the release steps. Then `make watch-accept`."
        ]
    report = "\n".join(lines)
    print(report)
    if report_path:
        Path(report_path).write_text(report + "\n")
    return 1 if changed or failed or new else 0


def accept() -> int:
    problems = 0
    for source in sources():
        if source.url in MANUAL:
            continue
        try:
            save(source, fetch(source.url))
            print(f"saved {source.url}")
        except Unreadable as error:
            problems += 1
            print(f"could not read {source.url}: {error}", file=sys.stderr)
    keep = {source.snapshot.name for source in sources()}
    for stale in SNAPSHOTS.glob("*.txt"):
        if stale.name not in keep:
            stale.unlink()
            print(f"removed {stale.name}: no pack cites it any more")
    return 1 if problems else 0


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("command", choices=["check", "accept"])
    parser.add_argument("--report")
    args = parser.parse_args()
    sys.exit(check(args.report) if args.command == "check" else accept())
