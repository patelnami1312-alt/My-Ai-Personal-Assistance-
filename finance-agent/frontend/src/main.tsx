import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { createBrowserRouter, Navigate, RouterProvider } from 'react-router-dom'
import './index.css'
import { Layout } from './components/Layout'
import { Dashboard } from './pages/Dashboard'
import { Chat } from './pages/Chat'
import { Transactions } from './pages/Transactions'
import { Accounts } from './pages/Accounts'
import { Budgets } from './pages/Budgets'
import { Bills } from './pages/Bills'

const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    children: [
      { index: true, element: <Dashboard /> },
      { path: 'chat', element: <Chat /> },
      { path: 'transactions', element: <Transactions /> },
      { path: 'accounts', element: <Accounts /> },
      { path: 'budgets', element: <Budgets /> },
      { path: 'bills', element: <Bills /> },
      { path: '*', element: <Navigate to="/" replace /> },
    ],
  },
])

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <RouterProvider router={router} />
  </StrictMode>,
)
