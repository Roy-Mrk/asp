import { useEffect, useState } from 'react';

export default function Home() {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [name, setName] = useState('');
  const [greet, setGreet] = useState(null);
  const [loginForm, setLoginForm] = useState({ username: '', password: '' });
  const [token, setToken] = useState(null);
  const [me, setMe] = useState(null);

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

  const handleLogin = async () => {
    const baseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5000';
    try {
      setMe(null);
      const res = await fetch(`${baseUrl}/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(loginForm),
      });
      if (!res.ok) throw new Error(await res.text());
      const json = await res.json();
      setToken(json.token);
    } catch (e) {
      alert(`Login failed: ${e}`);
    }
  };

  const handleMe = async () => {
    if (!token) return;
    const baseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5000';
    try {
      const res = await fetch(`${baseUrl}/auth/me`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (!res.ok) throw new Error(await res.text());
      const json = await res.json();
      setMe(json);
    } catch (e) {
      alert(`Me failed: ${e}`);
    }
  };

  return (
    <main style={{ fontFamily: 'system-ui', padding: 24 }}>
      <h1>Roy-Mrk's portfolio (Next.js + ASP.NET Core)</h1>
      <p><a href="/login">Go to Login</a></p>
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

      <section style={{ marginTop: 24 }}>
        <h2>Auth</h2>
        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
          <input
            placeholder="username"
            value={loginForm.username}
            onChange={(e) => setLoginForm({ ...loginForm, username: e.target.value })}
            style={{ padding: 8, border: '1px solid #ccc', borderRadius: 4 }}
          />
          <input
            placeholder="password"
            type="password"
            value={loginForm.password}
            onChange={(e) => setLoginForm({ ...loginForm, password: e.target.value })}
            style={{ padding: 8, border: '1px solid #ccc', borderRadius: 4 }}
          />
          <button onClick={handleLogin} style={{ padding: '8px 12px' }}>Login</button>
          <button onClick={handleMe} disabled={!token} style={{ padding: '8px 12px' }}>Me</button>
        </div>
        <div style={{ marginTop: 12 }}>
          {token && (
            <p>Token: <span style={{ wordBreak: 'break-all' }}>{token.slice(0, 12)}...{token.slice(-12)}</span></p>
          )}
          {me && (
            <pre style={{ background: '#f5f5f5', padding: 12 }}>
{JSON.stringify(me, null, 2)}
            </pre>
          )}
        </div>
      </section>
    </main>
  );
}

