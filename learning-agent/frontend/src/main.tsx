import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { createBrowserRouter, Navigate, RouterProvider } from 'react-router-dom'
import './index.css'
import { Layout } from './components/Layout'
import { Dashboard } from './pages/Dashboard'
import { Chat } from './pages/Chat'
import { Courses } from './pages/Courses'
import { StudyLog } from './pages/StudyLog'
import { Notes } from './pages/Notes'
import { Goals } from './pages/Goals'

const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    children: [
      { index: true, element: <Dashboard /> },
      { path: 'chat', element: <Chat /> },
      { path: 'courses', element: <Courses /> },
      { path: 'study-log', element: <StudyLog /> },
      { path: 'notes', element: <Notes /> },
      { path: 'goals', element: <Goals /> },
      { path: '*', element: <Navigate to="/" replace /> },
    ],
  },
])

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <RouterProvider router={router} />
  </StrictMode>,
)
