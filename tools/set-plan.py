"""Sets a company's plan by hand, for billing by invoice (ADR 0015).

    python3 tools/set-plan.py SLUG                  # show the company, its plan and any upgrade request
    python3 tools/set-plan.py SLUG growth           # set the plan, after the invoice is paid
    python3 tools/set-plan.py SLUG free             # back to free, for example when an invoice lapses

Setting a plan clears the company's upgrade request, so it drops off the daily
billing-requests issue. A company that pays by card through Stripe has its plan set by
Stripe's events; this refuses to change it unless --force is given.
"""

from __future__ import annotations

import sys

from registry_sql import literal, sql

PLANS = {"free", "growth", "business"}


def main() -> None:
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    flags = {a for a in sys.argv[1:] if a.startswith("--")}
    if len(args) not in (1, 2) or (len(args) == 2 and args[1] not in PLANS):
        sys.exit(__doc__)
    slug = args[0]

    rows = sql(f"select id, legal_name, slug, tier from registry.tenant where slug = {literal(slug)}")
    if not rows:
        sys.exit(f"No company {slug}.")
    company = rows[0]
    tenant_id = literal(company["id"])
    billing = sql(
        "select requested_tier, requested_interval, requested_currency, requested_by, requested_at, "
        f"stripe_subscription_id, subscription_status from registry.tenant_billing where tenant_id = {tenant_id}"
    )
    billing = billing[0] if billing else {}

    print(f"Company:  {company['legal_name']}  ({company['slug']})")
    print(f"Plan:     {company['tier']}")
    if billing.get("requested_tier"):
        print(
            f"Request:  {billing['requested_tier']}, {billing['requested_interval']}ly in "
            f"{billing['requested_currency'].upper()}, by {billing['requested_by']} on {billing['requested_at'][:10]}"
        )
    if billing.get("stripe_subscription_id"):
        print(f"Stripe:   {billing['stripe_subscription_id']} ({billing['subscription_status']})")
    if len(args) == 1:
        return

    plan = args[1]
    if billing.get("stripe_subscription_id") and "--force" not in flags:
        sys.exit("This company pays by card: change its plan in Stripe, or pass --force.")
    sql(f"update registry.tenant set tier = {literal(plan)} where id = {tenant_id}")
    sql(
        "update registry.tenant_billing set requested_tier = null, requested_interval = null, "
        "requested_currency = null, requested_by = null, requested_at = null, updated_at = now() "
        f"where tenant_id = {tenant_id}"
    )
    print(f"Set to {plan}. Their dashboard shows it on the next load; quotas apply at once.")


if __name__ == "__main__":
    main()
