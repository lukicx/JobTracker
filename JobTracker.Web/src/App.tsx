import { useState } from 'react'

function App() {
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

    const [jobs, setJobs] = useState<ExternalJob[]>([])
    const [myApplications, setMyApplications] = useState<JobApplication[]>([])
    
    const [search, setSearch] = useState('')
    const [location, setLocation] = useState('')

    async function handleSearch() {
        const params = new URLSearchParams({
            search, location
        })
        const response = await fetch('http://localhost:5290/api/jobs?' + params)
        const result: Array<ExternalJob> = await response.json()
        setJobs(result)
        
    }
    
    async function handleSave (job : ExternalJob){
        const request = {
            company: job.company,
            position: job.position,
            location: job.location,
            jobUrl: job.url,
            status: 'Interested'
        }
        console.log(request)
        const result = await fetch('http://localhost:5290/api/applications', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(request)
        })
        if (result.ok) {
            const body = await result.json()
            console.log(body)
        }
        else {
            const error = await result.text()

            console.log('Status:', result.status)
            console.log('Error:', error)
        }
        
    }

    return (
        <main>
            <h1>JobTracker</h1>

            <input
                type="text"
                placeholder="Search jobs"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
            />

            <input
                type="text"
                placeholder="Location"
                value={location}
                onChange={(event) => setLocation(event.target.value)}
            />

            <button onClick={handleSearch}>
                Search
            </button>

            <div>
                {jobs.map(job => (
                    <div key={job.externalId}>
                        <h2>{job.position}</h2>
                        <p>{job.company}</p>
                        <p>{job.location}</p>
                        <a href={job.url}>Open</a>
                        <button
                            onClick={() => handleSave(job)}
                            disabled={!job.company.trim()}
                        >
                            Save
                        </button>
                    </div>
                ))}
            </div>
        </main>
    )
}

export default App