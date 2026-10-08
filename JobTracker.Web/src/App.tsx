import { useState } from 'react'
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
    const [token, setToken] = useState<string | null>(null)
    const [authMode, setAuthMode] = useState<'login' | 'register'>('login')
    
    const [jobs, setJobs] = useState<ExternalJob[]>([])
    const [myApplications, setMyApplications] = useState<JobApplication[]>([])

    const [search, setSearch] = useState('')
    const [location, setLocation] = useState('')

    const [view, setView] = useState<'search' | 'applications'>('search')

    const [loadingJobs, setLoadingJobs] = useState(false)
    const [loadingApplications, setLoadingApplications] = useState(false)

    const [error, setError] = useState<string | null>(null)
    const [message, setMessage] = useState<string | null>(null)

    async function handleSearch() {
        setError(null)
        setMessage(null)
        setLoadingJobs(true)

        try {
            const params = new URLSearchParams({
                search,
                location
            })

            const response = await fetch('/api/jobs?' + params)

            if (!response.ok) {
                setError(`Could not load jobs. HTTP ${response.status}`)
                return
            }

            const result: ExternalJob[] = await response.json()
            setJobs(result)
        }
        catch (error) {
            console.error('Request failed:', error)
            setError('Could not reach the API.')
        }
        finally {
            setLoadingJobs(false)
        }
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

            if (!response.ok) {
                setError(`Could not save application. HTTP ${response.status}`)
                return
            }

            const body: JobApplication = await response.json()

            console.log('Saved:', body)

            setMessage(`Saved ${body.position}.`)
        }
        catch (error) {
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
        catch (error) {
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
                    <section className="auth-card" aria-labelledby="auth-title">
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
                                autoComplete="email"
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
                                <button onClick={handleLogin}>Login</button>
                                <p className="auth-switch">
                                    New here?{' '}
                                    <button className="link-button" onClick={() => setAuthMode('register')}>Create account</button>
                                </p>
                            </div>
                        )}
                        {authMode === 'register' && (
                            <div className="auth-actions">
                                <button onClick={handleRegister}>Register</button>
                                <p className="auth-switch">
                                    Already have an account?{' '}
                                    <button className="link-button" onClick={() => setAuthMode('login')}>Back to login</button>
                                </p>
                            </div>
                        )}
                    </section>
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
                    <div className="search-bar">
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
                        <button onClick={handleSearch} disabled={loadingJobs}>Search</button>
                    </div>
                    {loadingJobs && <p className="loading-message" role="status">Loading jobs...</p>}
                    {!loadingJobs && jobs.length === 0 && (
                        <div className="empty-state">
                            <h2>Make room for your next opportunity.</h2>
                            <p className="muted">Search by role and location to discover jobs you can save.</p>
                        </div>
                    )}
                    <div className="card-grid">
                        {jobs.map(job => (
                            <article className="job-card" key={job.externalId}>
                                <p className="company-name">{job.company}</p>
                                <h2>{job.position}</h2>
                                {job.location && <p className="muted">{job.location}</p>}
                                <div className="card-actions">
                                    {job.url && <a href={job.url}>View job <span aria-hidden="true">↗</span></a>}
                                    <button onClick={() => handleSave(job)} disabled={!job.company.trim()}>Save</button>
                                </div>
                            </article>
                        ))}
                    </div>
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
                                    {application.jobUrl && <a href={application.jobUrl}>View job <span aria-hidden="true">↗</span></a>}
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
            onLogout={() => setSession(current => current + 1)}
        />
    )
}

export default App
