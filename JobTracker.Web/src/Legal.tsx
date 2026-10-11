import type { ReactNode } from 'react'

export type LegalPageKey = 'privacy' | 'terms' | 'cookies' | 'refunds'

const LAST_UPDATED = '11 October 2026'
const REPOSITORY_URL = 'https://github.com/lukicx/JobTracker'

function operatorValue(value: string | undefined, fallback: string) {
    const trimmed = value?.trim()
    return trimmed ? trimmed : fallback
}

const operatorName = operatorValue(
    import.meta.env.VITE_OPERATOR_NAME,
    'Operator name not configured'
)

const legalContact = operatorValue(
    import.meta.env.VITE_LEGAL_CONTACT_EMAIL,
    'Privacy contact not configured'
)

const businessDetails = operatorValue(
    import.meta.env.VITE_OPERATOR_BUSINESS_DETAILS,
    'No business details configured'
)

const hostingProvider = operatorValue(
    import.meta.env.VITE_HOSTING_PROVIDER,
    'Hosting provider not configured'
)

function LegalLayout({
    title,
    children
}: {
    title: string
    children: ReactNode
}) {
    return (
        <main className="app-shell legal-shell">
            <a className="skip-link" href="#legal-content">Skip to content</a>
            <header className="app-header">
                <a className="brand" href="#" aria-label="Back to JobTracker">
                    <span className="brand-mark" aria-hidden="true">JT</span>
                    <span>JobTracker</span>
                </a>
            </header>

            <article id="legal-content" className="legal-card">
                <p className="eyebrow">Legal</p>
                <h1>{title}</h1>
                <p className="legal-updated">Last updated: {LAST_UPDATED}</p>
                {children}
            </article>

            <LegalFooter />
        </main>
    )
}

function DeploymentNotice() {
    const isConfigured =
        !operatorName.includes('not configured') &&
        !legalContact.includes('not configured')

    if (isConfigured) {
        return null
    }

    return (
        <div className="notice notice-error" role="note">
            <strong>Before public production use:</strong> configure the real operator name
            and a private legal/privacy contact. Do not rely on this development notice as
            a substitute for legally required controller identification.
        </div>
    )
}

export function LegalFooter() {
    return (
        <footer className="legal-footer" aria-label="Legal information">
            <nav aria-label="Legal links">
                <a href="#privacy">Privacy</a>
                <a href="#terms">Terms</a>
                <a href="#cookies">Cookies</a>
                <a href="#refunds">Refunds</a>
            </nav>
            <p>
                JobTracker is an independent project. Job-search results are supplied via
                Jooble and should be verified on the original listing.
            </p>
        </footer>
    )
}

export function LegalPage({ page }: { page: LegalPageKey }) {
    if (page === 'privacy') {
        return (
            <LegalLayout title="Privacy Policy">
                <DeploymentNotice />

                <section>
                    <h2>Who operates JobTracker</h2>
                    <dl className="legal-details">
                        <div><dt>Operator / data controller</dt><dd>{operatorName}</dd></div>
                        <div><dt>Privacy contact</dt><dd>{legalContact}</dd></div>
                        <div><dt>Business details</dt><dd>{businessDetails}</dd></div>
                        <div><dt>Hosting provider</dt><dd>{hostingProvider}</dd></div>
                    </dl>
                </section>

                <section>
                    <h2>Data we use</h2>
                    <p>
                        JobTracker only asks for data needed to provide the service:
                        your email address, authentication data managed by ASP.NET Core
                        Identity, and the job-application information you choose to save
                        (for example company, role, location, status, job URL and creation
                        time).
                    </p>
                    <p>
                        The application code stores a password hash rather than your plain
                        password. Access tokens are kept in React memory only and are not
                        deliberately persisted in cookies or browser local storage.
                    </p>
                </section>

                <section>
                    <h2>Job search and Jooble</h2>
                    <p>
                        When you search for jobs, the search keywords and location you enter
                        are sent by the JobTracker server to the Czech Jooble API so the
                        requested results can be returned. JobTracker does not send your
                        account email or saved application records to Jooble through this
                        integration.
                    </p>
                    <p>
                        Job listings and destination links are third-party content. Review
                        Jooble's own privacy information and the privacy terms of any site
                        you visit from a job listing.
                    </p>
                </section>

                <section>
                    <h2>Why data is processed</h2>
                    <p>
                        Account and application data are processed because they are needed
                        to provide the account and job-tracking service you request.
                        Technical security or request logs, if enabled by the production
                        host, may be processed for security, abuse prevention and service
                        reliability. The production operator must document the actual
                        hosting/logging setup before launch.
                    </p>
                </section>

                <section>
                    <h2>Retention and your choices</h2>
                    <p>
                        Saved applications remain until they are deleted. Account data
                        remains while the account is maintained, subject to any legal or
                        security retention obligations of the production operator. The
                        current project does not yet provide a self-service account-deletion
                        screen, so a private privacy contact must be configured before a
                        public launch so users can exercise applicable data-protection rights.
                    </p>
                </section>

                <section>
                    <h2>Your data-protection rights</h2>
                    <p>
                        Depending on applicable law, you may have rights to access, correct,
                        erase, restrict or receive your personal data, and to object to
                        certain processing. You may also have the right to complain to the
                        competent data-protection authority.
                    </p>
                </section>

                <section>
                    <h2>Analytics, cookies and embeds</h2>
                    <p>
                        The current JobTracker codebase contains no analytics tracker,
                        advertising pixel, third-party iframe/embed, cookie-setting code or
                        browser local-storage use. See the Cookie Policy for what must change
                        if any of these technologies are added later.
                    </p>
                </section>
            </LegalLayout>
        )
    }

    if (page === 'terms') {
        return (
            <LegalLayout title="Terms and Conditions">
                <DeploymentNotice />

                <section>
                    <h2>About the service</h2>
                    <p>
                        JobTracker is an independent job-search and application-tracking
                        project. It lets users search third-party job listings and save their
                        own application records. It is not an employer, recruiter or job
                        placement service.
                    </p>
                </section>

                <section>
                    <h2>Accounts</h2>
                    <p>
                        Provide accurate account information, keep your credentials secure,
                        and do not attempt to access another user's account or data. You are
                        responsible for activity performed through your account.
                    </p>
                </section>

                <section>
                    <h2>Third-party job listings</h2>
                    <p>
                        Search results are supplied via Jooble and may link to third-party
                        sites. JobTracker does not create, verify or guarantee those listings.
                        Always verify role details, employer identity, application terms and
                        privacy practices on the original site before relying on a listing.
                    </p>
                </section>

                <section>
                    <h2>Acceptable use</h2>
                    <p>
                        Do not misuse the service, interfere with its operation, probe or
                        bypass access controls, scrape it in a way that harms the service, or
                        use it unlawfully or to infringe third-party rights.
                    </p>
                </section>

                <section>
                    <h2>Availability and liability</h2>
                    <p>
                        The service is provided on an as-available basis and may change or be
                        unavailable. Nothing in these terms excludes or limits rights or
                        liability that cannot lawfully be excluded. To the extent permitted by
                        applicable law, the operator is not responsible for decisions made in
                        reliance on third-party job listings or external websites.
                    </p>
                </section>

                <section>
                    <h2>Intellectual property</h2>
                    <p>
                        JobTracker does not claim ownership of third-party job listings,
                        employer names or linked content. Users should not upload or submit
                        material they do not have the right to use.
                    </p>
                </section>

                <section>
                    <h2>Changes and contact</h2>
                    <p>
                        Material changes to these terms should be published before they take
                        effect. Operator and contact details are listed in the Privacy Policy.
                    </p>
                </section>
            </LegalLayout>
        )
    }

    if (page === 'cookies') {
        return (
            <LegalLayout title="Cookie Policy">
                <section>
                    <h2>Current cookie use</h2>
                    <p>
                        The current JobTracker application code does not set cookies, use
                        browser local storage, load analytics trackers or embed advertising
                        technology. Authentication uses bearer tokens held in application
                        memory for the active page session.
                    </p>
                </section>

                <section>
                    <h2>Is a cookie-consent banner needed?</h2>
                    <p>
                        Not for the current codebase, because it does not use non-essential
                        cookies or similar tracking technologies. A banner should not be
                        added merely for appearance.
                    </p>
                    <p>
                        If analytics, advertising, tracking pixels, third-party widgets or
                        other non-essential device storage are added later, they must remain
                        disabled until any legally required consent has been obtained, and
                        this policy must be updated.
                    </p>
                </section>

                <section>
                    <h2>Hosting infrastructure</h2>
                    <p>
                        A production hosting provider, CDN or security service may introduce
                        strictly necessary storage or request logging. The operator must
                        verify the actual production stack and list any such technologies
                        here before launch.
                    </p>
                </section>
            </LegalLayout>
        )
    }

    return (
        <LegalLayout title="Refund Policy">
            <section>
                <h2>No paid service at present</h2>
                <p>
                    JobTracker currently offers no paid subscription, paid digital content,
                    product purchase or other checkout flow, and the application code does
                    not collect payment information. There is therefore currently no payment
                    to refund.
                </p>
            </section>

            <section>
                <h2>If paid features are introduced</h2>
                <p>
                    A specific pricing, cancellation and refund policy must be published
                    before users are charged. Any mandatory consumer rights under applicable
                    law will continue to apply and will not be overridden by this policy.
                </p>
            </section>
        </LegalLayout>
    )
}
