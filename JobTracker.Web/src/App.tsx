import { useRef, useState } from 'react'
import './App.css'
import './index.css'

type ExternalJob = {
    externalId: string
    company: string
    position: string
    location?: string
    url?: string
    description?: string
}

type JobSource = 'jobscz' | 'jooble'
type SearchSource = 'all' | JobSource

type JobSearch = { search: string; location: string; source: SearchSource }
type JobSearchResponse = {
    jobs: ExternalJob[]
    nextJobsCzPage: number | null
    nextJooblePage: number | null
}

const sourceLabels = { jobscz: 'Jobs.cz', jooble: 'Jooble', other: 'Other' }
const jobsPerPage = 30

function Pagination({ page, pageCount, disabled, onChange, position }: {
    page: number
    pageCount: number
    disabled: boolean
    onChange: (page: number) => void
    position: string
}) {
    if (pageCount <= 1) return null

    const pages = Array.from({ length: pageCount }, (_, index) => index + 1)
        .filter(number => pageCount <= 7 || number === 1 || number === pageCount || Math.abs(number - page) <= 1)

    return (
        <nav className="pagination" aria-label={`Job results pages ${position}`}>
            <button className="button-secondary" disabled={disabled || page === 1} onClick={() => onChange(page - 1)}>Previous</button>
            {pages.map((number, index) => (
                <span className="pagination-item" key={number}>
                    {index > 0 && number - pages[index - 1] > 1 && <span className="pagination-gap" aria-hidden="true">…</span>}
                    <button
                        className="button-secondary page-number"
                        aria-label={`Page ${number}`}
                        aria-current={number === page ? 'page' : undefined}
                        disabled={disabled}
                        onClick={() => onChange(number)}
                    >{number}</button>
                </span>
            ))}
            <button className="button-secondary" disabled={disabled || page === pageCount} onClick={() => onChange(page + 1)}>Next</button>
        </nav>
    )
}

function getJobSource(job: ExternalJob): JobSource | 'other' {
    if (job.externalId.startsWith('jobscz:')) return 'jobscz'
    if (job.externalId.startsWith('jooble:')) return 'jooble'
    return 'other'
}

type JobApplication = {
    id: number
    company: string
    position: string
    status: string
    location?: string
    jobUrl?: string
    createdAt: string
}

async function getAuthError(response: Response, fallback: string): Promise<string> {
    const problem: {
        detail?: string
        errors?: Record<string, string[]>
    } | null = await response.json().catch(() => null)

    const validationErrors = Object.values(problem?.errors ?? {}).flat()
    if (validationErrors.length > 0) {
        return validationErrors.join(' ')
    }

    if (response.status === 401) {
        switch (problem?.detail) {
            case 'LockedOut':
                return 'Your account is temporarily locked. Please try again later.'
            case 'RequiresTwoFactor':
                return 'This account requires a two-factor authentication code.'
            case 'NotAllowed':
                return 'Login is not allowed for this account. Check whether email confirmation is required.'
            default:
                return 'Login failed. Check your email and password, and make sure you have registered an account.'
        }
    }

    return fallback
}


function JobTracker({ onLogout }: { onLogout: () => void }) {
    const [email, setEmail] = useState('')
    const [pwd, setPwd] = useState('')
    const [token, setToken] = useState<string | null>(() => sessionStorage.getItem('token'))
    const [authMode, setAuthMode] = useState<'login' | 'register'>('login')
    
    const [jobs, setJobs] = useState<ExternalJob[]>([])
    const [jobSource, setJobSource] = useState<SearchSource>('all')
    const [submittedSearch, setSubmittedSearch] = useState<JobSearch | null>(null)
    const [nextPages, setNextPages] = useState<{ jobscz: number | null; jooble: number | null }>({ jobscz: null, jooble: null })
    const [myApplications, setMyApplications] = useState<JobApplication[]>([])

    const [search, setSearch] = useState('')
    const [location, setLocation] = useState('')

    const [view, setView] = useState<'search' | 'applications'>('search')

    const [loadingJobs, setLoadingJobs] = useState(false)
    const [page, setPage] = useState(1)
    const [pendingPage, setPendingPage] = useState<number | null>(null)
    const resultsRef = useRef<HTMLDivElement>(null)
    const [loadingApplications, setLoadingApplications] = useState(false)

    const [error, setError] = useState<string | null>(null)
    const [message, setMessage] = useState<string | null>(null)

    const hasSearched = submittedSearch !== null
    const resultSource = submittedSearch?.source ?? 'all'
    const hasMore = nextPages.jobscz !== null || nextPages.jooble !== null
    const pageCount = Math.max(1, Math.ceil(jobs.length / jobsPerPage) + (hasMore ? 1 : 0))
    const visibleJobs = jobs.slice((page - 1) * jobsPerPage, page * jobsPerPage)

    function scrollToResults() {
        requestAnimationFrame(() => {
            resultsRef.current?.focus({ preventScroll: true })
            resultsRef.current?.scrollIntoView({ block: 'start' })
        })
    }

    async function handleSearch() {
        if (loadingJobs) return
        const query = { search, location, source: jobSource }

        setError(null)
        setMessage(null)
        setLoadingJobs(true)
        setPendingPage(null)

        try {
            const params = new URLSearchParams(query)
            const response = await fetch('/api/jobs?' + params)

            if (!response.ok) {
                const problem: { detail?: string } | null = await response.json().catch(() => null)
                setError(problem?.detail ?? `Could not load jobs. HTTP ${response.status}`)
                return
            }

            const result: JobSearchResponse = await response.json()
            setJobs(result.jobs)
            setSubmittedSearch(query)
            setPage(1)
            setNextPages({ jobscz: result.nextJobsCzPage, jooble: result.nextJooblePage })
            setError(response.headers.get('X-Job-Search-Warning'))
            scrollToResults()
        }
        catch (error) {
            console.error('Request failed:', error)
            setError('Could not reach the API.')
        }
        finally {
            setLoadingJobs(false)
        }
    }

    async function changePage(requestedPage: number) {
        if (!submittedSearch || loadingJobs || requestedPage < 1 || requestedPage > pageCount || requestedPage === page) return

        setError(null)
        setMessage(null)
        let availableJobs = jobs
        let cursors = nextPages

        // Fill the displayed page from provider batches; keep visited jobs in memory.
        if (availableJobs.length < requestedPage * jobsPerPage && hasMore) {
            setLoadingJobs(true)
            setPendingPage(requestedPage)
            try {
                while (availableJobs.length < requestedPage * jobsPerPage && (cursors.jobscz !== null || cursors.jooble !== null)) {
                    const params = new URLSearchParams(submittedSearch)
                    params.set('jobsCzPage', String(cursors.jobscz ?? 0))
                    params.set('jooblePage', String(cursors.jooble ?? 0))
                    const response = await fetch('/api/jobs?' + params)
                    if (!response.ok) {
                        const problem: { detail?: string } | null = await response.json().catch(() => null)
                        setError(problem?.detail ?? `Could not load jobs. HTTP ${response.status}`)
                        return
                    }

                    const result: JobSearchResponse = await response.json()
                    const ids = new Set(availableJobs.map(job => job.externalId))
                    const newJobs = result.jobs.filter(job => {
                        if (ids.has(job.externalId)) return false
                        ids.add(job.externalId)
                        return true
                    })
                    availableJobs = [...availableJobs, ...newJobs]
                    cursors = { jobscz: result.nextJobsCzPage, jooble: result.nextJooblePage }
                    setJobs(availableJobs)
                    setNextPages(cursors)

                    const warning = response.headers.get('X-Job-Search-Warning')
                    setError(warning)
                    // Preserve failed provider cursors for the next navigation attempt.
                    if (warning) break
                }
            } catch {
                setError('Could not reach the API.')
                return
            } finally {
                setLoadingJobs(false)
                setPendingPage(null)
            }
        }

        // Providers can end with an empty or duplicate-only batch.
        setPage(Math.min(requestedPage, Math.max(1, Math.ceil(availableJobs.length / jobsPerPage))))
        scrollToResults()
    }

    async function handleSave(job: ExternalJob) {
        setError(null)
        setMessage(null)

        const request = {
            company: job.company,
            position: job.position,
            location: job.location,
            jobUrl: job.url,
            status: 'Interested'
        }

        try {
            const response = await fetch(
                '/api/applications',
                {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization' : `Bearer ${token}`
                    },
                    body: JSON.stringify(request)
                }
            )
            if(response.status == 409){
                setMessage(`You already saved this job.`)
                return
            }
            if (!response.ok) {
                setError(`Could not save application. HTTP ${response.status}`)
                return
            }

            const body: JobApplication = await response.json()

            console.log('Saved:', body)

            setMessage(`Saved ${body.position}.`)
        }
        catch {
            setError('Could not reach the API.')
        }
    }

    async function showMyApplications() {
        setError(null)
        setMessage(null)
        setLoadingApplications(true)

        try {
            const response = await fetch('/api/applications',
                {
                    
                method: 'GET',
                headers:
                    {
                        'Content-Type': 'application/json',
                        'Authorization' : `Bearer ${token}`
                    }
                }
            )

            if (!response.ok) {
                setError(`Could not load applications. HTTP ${response.status}`)
                return
            }

            const result: JobApplication[] = await response.json()
            setMyApplications(result)
        }
        catch (error) {
            console.error('Could not reach API:', error)
            setError('Could not reach the API.')
        }
        finally {
            setLoadingApplications(false)
        }
    }

    async function handleRemove(id: number) {
        setError(null)
        setMessage(null)

        try {
            const response = await fetch(
                `/api/applications/${id}`,
                {
                    method: 'DELETE',
                    headers:
                        {
                            'Content-Type': 'application/json',
                            'Authorization' : `Bearer ${token}`
                        }
                }
            )

            if (!response.ok) {
                setError(`Could not remove application. HTTP ${response.status}`)
                return
            }

            setMyApplications(current =>
                current.filter(application => application.id !== id)
            )
            setMessage('Application removed.')
        }
        catch {
            setError('Could not reach the API.')
        }
    }

    async function changeStatus(id: number, status: string) {
        setError(null)
        setMessage(null)

        try {
            const response = await fetch(
                `/api/applications/${id}/status`,
                {
                    method: 'PATCH',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization' : `Bearer ${token}`
                    },
                    body: JSON.stringify({ status })
                }
            )

            if (!response.ok) {
                setError(`Could not update status. HTTP ${response.status}`)
                return
            }

            setMyApplications(current =>
                current.map(application =>
                    application.id === id
                        ? { ...application, status }
                        : application
                )
            )

            setMessage('Status updated.')
        }
        catch (error) {
            console.error('Could not reach API:', error)
            setError('Could not reach the API.')
        }
    }

    async function openApplications() {
        setView('applications')
        await showMyApplications()
    }

    function openSearch() {
        setError(null)
        setMessage(null)
        setView('search')
    }
    
    async function handleLogin(){
        setError(null)
        setMessage(null)
        try {
            const response = await fetch('/api/login',
                {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({ email: email.trim(), password: pwd })
                })
            if (!response.ok){
                setError(await getAuthError(response, `Could not log in. HTTP ${response.status}`))
                return
            }
            const result = await response.json()
            setToken(result.accessToken)
            sessionStorage.setItem('token', result.accessToken)
            setMessage('Logged in successfully.')
        }
        catch (error){
            console.error('Could not reach API:', error)
            setError('Could not reach the API.')
        }
        
    }
    async function handleRegister(){
        setError(null)
        setMessage(null)
        try {
            const response = await fetch('/api/register',
                {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ email: email.trim(), password: pwd })
                })
            if (!response.ok){
                setError(await getAuthError(response, `Could not register. HTTP ${response.status}`))
                return
            }
            setAuthMode('login')
            setMessage('Account created successfully. Log in with your email and password.')
        }
        catch (error){
            console.error('Could not reach API:', error)
            setError('Could not reach the API.')
        }
        
    }

    if (!token) {
        return (
            <main className="app-shell auth-shell">
                <header className="app-header">
                    <div className="brand">
                        <span className="brand-mark" aria-hidden="true">JT</span>
                        <span>JobTracker</span>
                    </div>
                </header>
                <div className="auth-layout">
                    <section className="auth-intro">
                        <p className="eyebrow">One place for your job search</p>
                        <h1>Your next move,<br /><span>organized.</span></h1>
                        <p className="intro-description">
                            Find opportunities, save the roles you like, and keep
                            track of every application.
                        </p>
                        <div className="intro-note">From the first interesting role to your next offer.</div>
                    </section>
                    <form
                        className="auth-card"
                        aria-labelledby="auth-title"
                        onSubmit={event => {
                            event.preventDefault()
                            if (authMode === 'login') handleLogin()
                            else handleRegister()
                        }}
                    >
                        <h2 id="auth-title">{authMode === 'login' ? 'Welcome back' : 'Create your account'}</h2>
                        <p className="muted auth-description">
                            {authMode === 'login'
                                ? 'Log in to continue your job search.'
                                : 'Start keeping your applications in one place.'}
                        </p>
                        {error && <p className="notice notice-error" role="alert">{error}</p>}
                        {message && <p className="notice notice-success" role="status">{message}</p>}
                        <div className="field">
                            <label htmlFor="email">Email address</label>
                            <input
                                id="email"
                                type="email"
                                placeholder="you@example.com"
                                autoComplete="username"
                                value={email}
                                onChange={event => setEmail(event.target.value)}
                            />
                        </div>
                        <div className="field">
                            <label htmlFor="password">Password</label>
                            <input
                                id="password"
                                type="password"
                                placeholder="Enter your password"
                                autoComplete={authMode === 'login' ? 'current-password' : 'new-password'}
                                value={pwd}
                                onChange={event => setPwd(event.target.value)}
                            />
                        </div>
                        {authMode === 'login' && (
                            <div className="auth-actions">
                                <button type="submit">Login</button>
                                <p className="auth-switch">
                                    New here?{' '}
                                    <button type="button" className="link-button" onClick={() => setAuthMode('register')}>Create account</button>
                                </p>
                            </div>
                        )}
                        {authMode === 'register' && (
                            <div className="auth-actions">
                                <button type="submit">Register</button>
                                <p className="auth-switch">
                                    Already have an account?{' '}
                                    <button type="button" className="link-button" onClick={() => setAuthMode('login')}>Back to login</button>
                                </p>
                            </div>
                        )}
                    </form>
                </div>
            </main>
        )
    }

    return (
        <main className="app-shell">
            <header className="app-header">
                <div className="brand">
                    <span className="brand-mark" aria-hidden="true">JT</span>
                    <span>JobTracker</span>
                </div>
                <button className="button-secondary" onClick={onLogout}>Log out</button>
            </header>
            <nav className="view-tabs" aria-label="Main navigation">
                <button aria-pressed={view === 'search'} onClick={openSearch}>Search Jobs</button>
                <button aria-pressed={view === 'applications'} onClick={openApplications}>My Applications</button>
            </nav>
            {error && <p className="notice notice-error" role="alert">{error}</p>}
            {message && <p className="notice notice-success" role="status">{message}</p>}
            {view === 'search' && (
                <section aria-labelledby="search-title">
                    <div className="section-heading">
                        <p className="eyebrow">Explore opportunities</p>
                        <h1 id="search-title">Find your next role.</h1>
                        <p className="muted">A good opportunity is a search away.</p>
                    </div>
                    <form
                        className="search-bar"
                        onSubmit={event => {
                            event.preventDefault()
                            handleSearch()
                        }}
                    >
                        <div className="field">
                            <label htmlFor="job-search">Role or keyword</label>
                            <input
                                id="job-search"
                                type="text"
                                placeholder="e.g. Junior .NET developer"
                                value={search}
                                onChange={event => setSearch(event.target.value)}
                            />
                        </div>
                        <div className="field">
                            <label htmlFor="job-location">Location</label>
                            <input
                                id="job-location"
                                type="text"
                                placeholder="e.g. Brno"
                                value={location}
                                onChange={event => setLocation(event.target.value)}
                            />
                        </div>
                        <div className="field">
                            <label htmlFor="job-source">Source</label>
                            <select
                                id="job-source"
                                value={jobSource}
                                onChange={event => setJobSource(event.target.value as SearchSource)}
                                disabled={loadingJobs}
                            >
                                <option value="all">All sources</option>
                                <option value="jobscz">Jobs.cz</option>
                                <option value="jooble">Jooble</option>
                            </select>
                        </div>
                        <button type="submit" disabled={loadingJobs || !search.trim()}>Search</button>
                    </form>
                    <div ref={resultsRef} className="search-results-start" tabIndex={-1}>
                        {loadingJobs && <p className="loading-message" role="status">{pendingPage === null ? 'Loading jobs...' : `Loading page ${pendingPage}...`}</p>}
                        {hasSearched && (
                            <p className="search-result-count muted" role="status">
                                {jobs.length === 0 ? 'No results' : `Page ${page} · Jobs ${(page - 1) * jobsPerPage + 1}–${(page - 1) * jobsPerPage + visibleJobs.length}`}
                                {resultSource === 'all' ? ' from all sources' : ` from ${sourceLabels[resultSource]}`}
                            </p>
                        )}
                    </div>
                    {!loadingJobs && submittedSearch && (jobSource !== resultSource || search !== submittedSearch.search || location !== submittedSearch.location) && (
                        <p className="search-result-count muted">Press Search to apply these changes. Page navigation uses your previous search.</p>
                    )}
                    {jobs.length > 0 && <Pagination page={page} pageCount={pageCount} disabled={loadingJobs} onChange={changePage} position="above results" />}
                    {!loadingJobs && jobs.length === 0 && (
                        <div className="empty-state">
                            <h2>{!hasSearched
                                ? 'Make room for your next opportunity.'
                                : resultSource === 'all' ? 'No jobs found.' : `No ${sourceLabels[resultSource]} results.`}</h2>
                            <p className="muted">{!hasSearched
                                ? 'Search by role and location to discover jobs you can save.'
                                : resultSource === 'all'
                                    ? 'Try another role or location.'
                                    : 'Try another keyword or choose All sources.'}</p>
                        </div>
                    )}
                    <div className="card-grid">
                        {visibleJobs.map(job => (
                            <article className="job-card" key={job.externalId}>
                                <div className="job-card-heading">
                                    <p className="company-name">{job.company}</p>
                                    <span className="job-source">{sourceLabels[getJobSource(job)]}</span>
                                </div>
                                <h2>{job.position}</h2>
                                {job.location && <p className="muted">{job.location}</p>}
                                <div className="card-actions">
                                    {job.url && <a href={job.url} target="_blank" rel="noreferrer">View job <span aria-hidden="true">↗</span></a>}
                                    <button onClick={() => handleSave(job)} disabled={!job.company.trim()}>Save</button>
                                </div>
                            </article>
                        ))}
                    </div>
                    {jobs.length > 0 && <Pagination page={page} pageCount={pageCount} disabled={loadingJobs} onChange={changePage} position="below results" />}
                </section>
            )}
            {view === 'applications' && (
                <section aria-labelledby="applications-title">
                    <div className="section-heading">
                        <p className="eyebrow">Your progress, in one place</p>
                        <h1 id="applications-title">My applications.</h1>
                        <p className="muted">Keep track of each opportunity and its next step.</p>
                    </div>
                    {loadingApplications && <p className="loading-message" role="status">Loading applications...</p>}
                    {!loadingApplications && myApplications.length === 0 && (
                        <div className="empty-state">
                            <h2>No saved applications yet.</h2>
                            <p className="muted">Save a role from Search Jobs to start tracking it here.</p>
                        </div>
                    )}
                    <div className="card-grid">
                        {myApplications.map(application => (
                            <article className="job-card" key={application.id}>
                                <p className="company-name">{application.company}</p>
                                <h2>{application.position}</h2>
                                {application.location && <p className="muted">{application.location}</p>}
                                <div className="field application-status">
                                    <label htmlFor={`status-${application.id}`}>Status</label>
                                    <select
                                        className="status-select"
                                        data-status={application.status}
                                        id={`status-${application.id}`}
                                        value={application.status}
                                        onChange={event => changeStatus(application.id, event.target.value)}
                                    >
                                        <option value="Interested">Interested</option>
                                        <option value="Applied">Applied</option>
                                        <option value="Interview">Interview</option>
                                        <option value="Offer">Offer</option>
                                        <option value="Rejected">Rejected</option>
                                    </select>
                                </div>
                                <div className="card-actions">
                                    {application.jobUrl && <a href={application.jobUrl} target="_blank" rel="noreferrer">View job <span aria-hidden="true">↗</span></a>}
                                    <button className="button-danger" onClick={() => handleRemove(application.id)}>Remove</button>
                                </div>
                            </article>
                        ))}
                    </div>
                </section>
            )}
        </main>
    )
}

function App() {
    const [session, setSession] = useState(0)

    return (
        <JobTracker
            key={session}
            onLogout={() =>
            {
                sessionStorage.removeItem('token')
                setSession(current => current + 1)
            }}
        />
    )
}

export default App
