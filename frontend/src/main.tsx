import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { createBrowserRouter, Navigate, RouterProvider } from 'react-router-dom'
import './index.css'
import { Layout } from './components/Layout'
import { Home } from './agents/personal/Home'
import { Assistant } from './agents/personal/Assistant'
import { Tasks } from './agents/personal/Tasks'
import { Calendar } from './agents/personal/Calendar'
import { Notes } from './agents/personal/Notes'
import { Memory } from './agents/personal/Memory'
import { Settings } from './agents/personal/Settings'

const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    children: [
      { index: true, element: <Home /> },
      { path: 'assistant', element: <Assistant /> },
      { path: 'tasks', element: <Tasks /> },
      { path: 'calendar', element: <Calendar /> },
      { path: 'notes', element: <Notes /> },
      { path: 'memory', element: <Memory /> },
      { path: 'settings', element: <Settings /> },
      { path: '*', element: <Navigate to="/" replace /> },
    ],
  },
])

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <RouterProvider router={router} />
  </StrictMode>,
)
