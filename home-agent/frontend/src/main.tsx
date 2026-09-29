import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { createBrowserRouter, Navigate, RouterProvider } from 'react-router-dom'
import './index.css'
import { Layout } from './components/Layout'
import { Dashboard } from './pages/Dashboard'
import { Chat } from './pages/Chat'
import { Chores } from './pages/Chores'
import { Maintenance } from './pages/Maintenance'
import { Inventory } from './pages/Inventory'

const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    children: [
      { index: true, element: <Dashboard /> },
      { path: 'chat', element: <Chat /> },
      { path: 'chores', element: <Chores /> },
      { path: 'maintenance', element: <Maintenance /> },
      { path: 'inventory', element: <Inventory /> },
      { path: '*', element: <Navigate to="/" replace /> },
    ],
  },
])

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <RouterProvider router={router} />
  </StrictMode>,
)
