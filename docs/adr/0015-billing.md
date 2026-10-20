# ADR 0015 — Billing: invoices first, Stripe when there is an account

- **Status:** Accepted
- **Date:** 20 October 2026
- **Deciders:** Eoin

## Context

Paid plans are due by the 2 December launch. There is no Stripe account yet and no company to hold one (AivaStack Ltd is not registered). The first customers will be few, and each will want a conversation anyway, so a hand-sent invoice is enough to start; the code should not need to change when card payment arrives.

## Decision

- **The plan stays on the tenant** (`tenant.tier`: `free`, `growth`, `business`), which the quotas already read. A new `tenant_billing` row per tenant holds the way to it: an upgrade request (plan, monthly or yearly, euro or dollars, who asked, when), or the Stripe customer and subscription with Stripe's status.
- **Growth is the only plan bought from the dashboard.** Business and above are arranged by conversation. `POST /v1/account/upgrade` takes `{tier, interval, currency}`.
- **By invoice, while `Billing:Provider` is unset.** The call records the request. The view `registry.upgrade_requests_pending` lists open requests; a daily workflow in the private repository keeps one `billing-request` issue up to date from it, through the read-only `review_watch` role. Eoin invoices, and after payment runs `tools/set-plan.py SLUG growth`, which sets the plan and clears the request.
- **By card, when `Billing:Provider` is `stripe`.** The same call returns a Stripe Checkout page for the price `Billing:Stripe:Prices:{tier}:{interval}:{currency}` (one Stripe price per plan, interval and currency, rather than relying on Stripe's currency conversion), with the tenant in `client_reference_id` and in the metadata. `POST /v1/account/billing/portal` opens Stripe's customer portal. `POST /webhooks/stripe` checks the `Stripe-Signature` header as Stripe documents it (HMAC-SHA256 of `{t}.{body}` with the endpoint secret, `v1` only, constant-time comparison, five minutes' tolerance; read 20 Oct 2026) and acts on `checkout.session.completed` and `customer.subscription.created`, `.updated` and `.deleted`: the plan is kept while the subscription is `active`, `trialing` or `past_due`, and returns to free otherwise. Each event sets the state it describes, so a repeat delivery changes nothing. Stripe is called over its REST API with no SDK.
- **Prices** (Eoin, 20 Oct 2026; the same numbers in euro and dollars, excluding VAT): Growth 449 a month or 4,490 a year, 50,000 outputs a month and then 5 per 1,000; Business 1,490 a month, 250,000 outputs, then 5 per 1,000; own-name onboarding 1,500 once, waived on yearly plans. The first ten Growth customers pay 249 a month for twelve months: a Stripe promotion code at Checkout, or the invoice price.
- **Overage.** Plans with `Quotas:Overage:{tier}` keep signing past their allowance; the dashboard shows the extra outputs.

## Consequences

- Nothing is charged automatically yet. Overage and the onboarding fee go on invoices by hand until Stripe usage metering and one-off invoice items are set up.
- Going live with cards needs: the Stripe account; the products and the four Growth prices (monthly and yearly, in euro and dollars); the founding promotion code; a webhook endpoint at `https://<api>/webhooks/stripe` for the four event types; the customer portal configured; and `Billing:Provider`, `Billing:Stripe:SecretKey`, `Billing:Stripe:WebhookSecret` and the price ids as Fly.io secrets. VAT handling (Stripe Tax or otherwise) is decided then.
- Tested against a stand-in Stripe (the fake upstream used for fal and DNS), including forged and stale signatures; not yet against Stripe itself.

## Open

- VAT and invoicing entity, once AivaStack Ltd exists.
- Automatic overage billing (Stripe meters).
- Whether the founding offer is honoured on yearly plans.
