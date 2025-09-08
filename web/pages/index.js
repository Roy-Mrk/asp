import { useEffect, useState } from 'react';

export default function Home() {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const baseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5000';
    fetch(`${baseUrl}/ping`)
      .then(async (res) => {
        if (!res.ok) throw new Error(await res.text());
        return res.json();
      })
      .then((json) => setData(json))
      .catch((e) => setError(String(e)))
      .finally(() => setLoading(false));
  }, []);

  return (
    <main style={{ fontFamily: 'system-ui', padding: 24 }}>
      <h1>ChatGPT App (Next.js + ASP.NET Core)</h1>
      <p>API Base: {process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5000'}</p>
      {loading && <p>Loading...</p>}
      {error && <p style={{ color: 'crimson' }}>Error: {error}</p>}
      {data && (
        <pre style={{ background: '#f5f5f5', padding: 12 }}>
{JSON.stringify(data, null, 2)}
        </pre>
      )}
    </main>
  );
}

