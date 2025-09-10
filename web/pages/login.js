import { useState } from 'react';

export default function LoginPage() {
  const [form, setForm] = useState({ username: '', password: '' });
  const [token, setToken] = useState(null);
  const [me, setMe] = useState(null);
  const baseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5000';

  const doLogin = async (e) => {
    e.preventDefault();
    try {
      setMe(null);
      const res = await fetch(`${baseUrl}/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(form),
      });
      if (!res.ok) throw new Error(await res.text());
      const json = await res.json();
      setToken(json.token);
    } catch (e) {
      alert(`Login failed: ${e}`);
    }
  };

  const getMe = async () => {
    if (!token) return;
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
    <main style={{ fontFamily: 'system-ui', padding: 24, maxWidth: 520, margin: '0 auto' }}>
      <h1>Login</h1>
      <form onSubmit={doLogin} style={{ display: 'grid', gap: 12 }}>
        <input
          placeholder="username"
          value={form.username}
          onChange={(e) => setForm({ ...form, username: e.target.value })}
          style={{ padding: 10, border: '1px solid #ccc', borderRadius: 6 }}
        />
        <input
          placeholder="password"
          type="password"
          value={form.password}
          onChange={(e) => setForm({ ...form, password: e.target.value })}
          style={{ padding: 10, border: '1px solid #ccc', borderRadius: 6 }}
        />
        <button type="submit" style={{ padding: '10px 14px' }}>Login</button>
      </form>

      {token && (
        <section style={{ marginTop: 16 }}>
          <p>Token: <span style={{ wordBreak: 'break-all' }}>{token.slice(0, 12)}...{token.slice(-12)}</span></p>
          <button onClick={getMe} style={{ padding: '8px 12px' }}>Me</button>
          {me && (
            <pre style={{ background: '#f5f5f5', padding: 12, marginTop: 12 }}>
{JSON.stringify(me, null, 2)}
            </pre>
          )}
        </section>
      )}

      <p style={{ marginTop: 24 }}>
        <a href="/">← Back to Home</a>
      </p>
    </main>
  );
}
