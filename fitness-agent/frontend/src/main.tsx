import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { createBrowserRouter, Navigate, RouterProvider } from 'react-router-dom'
import './index.css'
import { Layout } from './components/Layout'
import { Dashboard } from './pages/Dashboard'
import { Chat } from './pages/Chat'
import { Workouts } from './pages/Workouts'
import { BodyWeight } from './pages/BodyWeight'
import { Nutrition } from './pages/Nutrition'
import { Goals } from './pages/Goals'

const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    children: [
      { index: true, element: <Dashboard /> },
      { path: 'chat', element: <Chat /> },
      { path: 'workouts', element: <Workouts /> },
      { path: 'bodyweight', element: <BodyWeight /> },
      { path: 'nutrition', element: <Nutrition /> },
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
