#!/usr/bin/env bash
# Runs the Supabase CLI as SignUnder's own account, without touching the global login.
#
#   tools/supabase.sh projects list
#   tools/supabase.sh <any supabase command>
#
# Reads SUPABASE_ACCESS_TOKEN from .env at the repository root (git ignores it), and
# defaults SUPABASE_PROJECT_REF to the SignUnder project (SIGNUNDER.md §4.9).
set -euo pipefail
cd "$(dirname "$0")/.."

token=$(sed -n 's/^SUPABASE_ACCESS_TOKEN=//p' .env 2>/dev/null | tail -1)
if [ -z "$token" ]; then
  echo "tools/supabase.sh: set SUPABASE_ACCESS_TOKEN in .env (Supabase dashboard → Account → Access Tokens)" >&2
  exit 2
fi
SUPABASE_ACCESS_TOKEN=$token SUPABASE_PROJECT_REF=${SUPABASE_PROJECT_REF:-lqxxcrkimjbysfqmedmt} \
  exec supabase "$@"
