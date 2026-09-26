import { useState, useEffect } from 'react';

function App() {
  const [apiMessage, setApiMessage] = useState('Waiting for API...');

  useEffect(() => {
    fetch('/api/hello')
      .then((response) => response.text())
      .then((data) => setApiMessage(data))
      .catch((error) => setApiMessage('Connection failed: ' + error.message));
  }, []);

  return (
    <>
      <h1>Welcome to the App</h1>
      <p>This is a simple React app.</p>

      <div style={{ marginTop: '20px', padding: '15px', border: '1px solid #ccc', borderRadius: '8px' }}>
        <h2>API Connection Test:</h2>
        <p>{apiMessage}</p>
      </div>
    </>
  );
}

export default App;