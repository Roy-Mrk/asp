import { useEffect, useState } from 'react';

export default function Home() {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [name, setName] = useState('');
  const [greet, setGreet] = useState(null);

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

  const handleGreet = async () => {
    const baseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5000';
    try {
      setGreet({ loading: true });
      const url = new URL(baseUrl + '/greet');
      if (name) url.searchParams.set('name', name);
      const res = await fetch(url.toString());
      if (!res.ok) throw new Error(await res.text());
      const json = await res.json();
      setGreet({ data: json });
    } catch (e) {
      setGreet({ error: String(e) });
    }
  };

  return (
    <main style={{ fontFamily: 'system-ui', padding: 24 }}>
      <h1>Roy-Mrk's portfolio (Next.js + ASP.NET Core)</h1>
      <p>API Base: {process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5000'}</p>
      {loading && <p>Loading...</p>}
      {error && <p style={{ color: 'crimson' }}>Error: {error}</p>}
      {data && (
        <pre style={{ background: '#f5f5f5', padding: 12 }}>
{JSON.stringify(data, null, 2)}
        </pre>
      )}

      <section style={{ marginTop: 24 }}>
        <h2>Greet</h2>
        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
          <input
            placeholder="Your name (optional)"
            value={name}
            onChange={(e) => setName(e.target.value)}
            style={{ padding: 8, border: '1px solid #ccc', borderRadius: 4 }}
          />
          <button onClick={handleGreet} style={{ padding: '8px 12px' }}>Say Hello</button>
        </div>
        <div style={{ marginTop: 12 }}>
          {greet?.loading && <p>Loading...</p>}
          {greet?.error && <p style={{ color: 'crimson' }}>Error: {greet.error}</p>}
          {greet?.data && (
            <pre style={{ background: '#f5f5f5', padding: 12 }}>
{JSON.stringify(greet.data, null, 2)}
            </pre>
          )}
        </div>
      </section>
    </main>
  );
}

