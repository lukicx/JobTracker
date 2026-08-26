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

function App() {
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

            const response = await fetch(
                'http://localhost:5290/api/jobs?' + params
            )

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
                'http://localhost:5290/api/applications',
                {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
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
            const response = await fetch('http://localhost:5290/api/applications')

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
                `http://localhost:5290/api/applications/${id}`,
                {
                    method: 'DELETE'
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
                `http://localhost:5290/api/applications/${id}/status`,
                {
                    method: 'PATCH',
                    headers: {
                        'Content-Type': 'application/json'
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

    return (
        <main>
            <h1>JobTracker</h1>

            <button onClick={openSearch}>
                Search Jobs
            </button>

            <button onClick={openApplications}>
                My Applications
            </button>

            {error && <p>{error}</p>}
            {message && <p>{message}</p>}

            {view === 'search' && (
                <div>
                    <input
                        type="text"
                        placeholder="Search jobs"
                        value={search}
                        onChange={event => setSearch(event.target.value)}
                    />

                    <input
                        type="text"
                        placeholder="Location"
                        value={location}
                        onChange={event => setLocation(event.target.value)}
                    />

                    <button
                        onClick={handleSearch}
                        disabled={loadingJobs}
                    >
                        Search
                    </button>

                    {loadingJobs && <p>Loading jobs...</p>}

                    {jobs.map(job => (
                        <div key={job.externalId}>
                            <h2>{job.position}</h2>
                            <p>{job.company}</p>
                            <p>{job.location}</p>

                            {job.url && (
                                <a href={job.url}>
                                    Open
                                </a>
                            )}

                            <button
                                onClick={() => handleSave(job)}
                                disabled={!job.company.trim()}
                            >
                                Save
                            </button>
                        </div>
                    ))}
                </div>
            )}

            {view === 'applications' && (
                <div>
                    {loadingApplications && (
                        <p>Loading applications...</p>
                    )}

                    {!loadingApplications &&
                        myApplications.length === 0 && (
                            <p>No saved applications yet.</p>
                        )}

                    {myApplications.map(application => (
                        <div key={application.id}>
                            <h2>{application.position}</h2>
                            <p>{application.company}</p>
                            <p>{application.location}</p>

                            <label
                                htmlFor={`status-${application.id}`}
                            >
                                Status
                            </label>

                            <select
                                id={`status-${application.id}`}
                                value={application.status}
                                onChange={event =>
                                    changeStatus(
                                        application.id,
                                        event.target.value
                                    )
                                }
                            >
                                <option value="Interested">
                                    Interested
                                </option>
                                <option value="Applied">
                                    Applied
                                </option>
                                <option value="Interview">
                                    Interview
                                </option>
                                <option value="Offer">
                                    Offer
                                </option>
                                <option value="Rejected">
                                    Rejected
                                </option>
                            </select>

                            {application.jobUrl && (
                                <a href={application.jobUrl}>
                                    Open
                                </a>
                            )}

                            <button
                                onClick={() =>
                                    handleRemove(application.id)
                                }
                            >
                                Remove
                            </button>
                        </div>
                    ))}
                </div>
            )}
        </main>
    )
}

export default App