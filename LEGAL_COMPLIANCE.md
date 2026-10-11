# Legal, privacy and accessibility launch checklist

This document is a technical compliance audit of the current JobTracker codebase. It is not
a substitute for advice from a qualified lawyer, and the applicable national rules depend on
where the service operator is established and whether the service is run commercially.

## What the current code does

- Account registration collects an email address and password. ASP.NET Core Identity manages
  the authentication records and stores a password hash rather than the plain password.
- Signed-in users can save job application data: company, position, location, job URL, status
  and creation time.
- Job searches send the user's search keywords and location from the JobTracker server to the
  Czech Jooble REST API. Account email and saved application records are not sent to Jooble
  by this integration.
- The frontend keeps bearer access tokens in React memory. It does not intentionally persist
  them in browser local storage.
- No analytics SDK, advertising pixel, third-party iframe/embed or review/testimonial component
  is present in the audited frontend source.
- No checkout/payment flow is present.
- No content images are present in the audited frontend source, so there are currently no
  content-image alt-text or image-licensing items to remediate.

## Changes made in the compliance hardening branch

- Added Privacy Policy, Terms and Conditions, Cookie Policy and Refund Policy pages.
- Added a required registration acknowledgement for the Privacy Policy and Terms. This is an
  acknowledgement of the terms/privacy information, not GDPR consent for processing that is
  necessary to provide the account service.
- Added operator/contact/hosting placeholders controlled by public Vite environment variables.
- Added proper form semantics so login, registration and search work with keyboard submission.
- Added skip links, clearer button/link labels, external-link safety, focus treatment and
  screen-reader context for links that open in a new tab.
- Improved low-contrast secondary and placeholder text.
- Removed the stale favicon reference and improved page metadata.
- Added CSP and common browser security headers in the Nginx config and disabled Nginx access
  logging to avoid unnecessarily recording users' search query strings in that layer.
- Removed the hardcoded PostgreSQL password from Compose and replaced it with environment
  variables.

## Cookie-consent conclusion

The audited frontend does not use analytics, ads, browser local storage or other non-essential
tracking. A consent banner should therefore not be added just for appearance.

Before public deployment, inspect actual production response headers and browser storage.
ASP.NET framework configuration, a CDN, WAF, load balancer or hosting platform may add cookies
that are not visible in the frontend source. Strictly necessary storage may not require consent
under applicable ePrivacy rules, but it should still be disclosed. Any non-essential analytics,
advertising or tracking technology must be blocked until legally valid consent is obtained.

## GDPR / privacy items that must be completed before launch

1. Configure the real data-controller/operator name and a monitored privacy contact.
2. Add the operator's legally required business/contact details if JobTracker is operated as a
   trader/business.
3. Identify the production hosting/database/CDN providers and sign any required processor/data
   processing agreements.
4. Confirm where personal data is hosted and whether any international transfer mechanism is
   needed.
5. Set and document retention periods for account data, application data, backups and security
   logs.
6. Provide a workable process for access, correction, deletion, restriction, portability and
   objection requests. The current UI has no self-service account-delete feature.
7. Verify production logging. Search terms and location appear in the job-search query string,
   so request logging can itself collect personal information.
8. Confirm the lawful basis used for account/application processing and for any security logs.
9. Identify the competent supervisory authority based on the operator's establishment.

GDPR Article 13 requires, among other things, controller identity/contact details, purposes and
legal bases, recipients/categories of recipients, retention information and data-subject rights.

## Jooble / third-party content

The application uses the Jooble REST API and links users to the returned vacancy URLs.

Before public launch:

- Confirm the API key is for the correct regional Jooble domain.
- Review and comply with the current Jooble API Terms of Use, not only the API documentation.
- Keep the Jooble source disclosure in the UI.
- Do not copy third-party images, logos or full copyrighted vacancy content unless the API terms
  and the relevant rights allow it.
- Treat job listings as third-party information and avoid claims that JobTracker verifies or
  guarantees them.

## Accessibility

The branch improves keyboard access, labels, focus indication, skip links, status messaging and
text contrast. The current frontend contains no content images requiring alt text.

Before launch, still run an automated accessibility scanner and perform a manual keyboard and
screen-reader smoke test on the deployed build. Automated checks alone do not establish WCAG
or statutory compliance.

The EU Accessibility Act can apply to covered consumer services, including e-commerce services,
from 28 June 2025. It also contains exemptions for microenterprises providing services. Whether
JobTracker falls within scope depends on the actual commercial service and operator.

## Consumer / refund rules

There is currently no payment flow, so there is no current payment to refund. The Refund Policy
states this rather than inventing refund rights or limits.

If paid features, subscriptions or digital services are introduced, do not reuse the current
refund page unchanged. Add pricing, cancellation, withdrawal/refund information and other
mandatory consumer disclosures for the operator's jurisdiction before taking payment.

## National-law flag

The exact local layer cannot be finalized until the operator's country of establishment is
confirmed.

- If established in the Czech Republic, check the Czech GDPR implementation rules and the
  Electronic Communications Act cookie rules in addition to GDPR/ePrivacy.
- If established in Slovakia, check Act No. 18/2018 Coll. on personal-data protection and the
  Slovak electronic-communications rules in addition to GDPR/ePrivacy.

Do not publish the production site with the "not configured" legal placeholders.
