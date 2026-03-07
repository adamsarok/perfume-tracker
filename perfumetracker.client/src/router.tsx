import { createRootRoute, createRoute, createRouter } from '@tanstack/react-router'
import RootLayout from './app/layout'
import HomePage from './app/page'
import LoginPage from './app/login/page'
import RegisterPage from './app/register/page'
import PerfumesPage from './app/perfumes/page'
import NewPerfumePage from './app/perfumes/new-perfume/page'
import RecommendationsPage from './app/perfumes/recommendations/page'
import ChatAgentPage from './app/perfumes/agent/page'
import EditPerfumePage from './app/perfumes/[id]/page'
import StatsPage from './app/stats/page'
import TagsPage from './app/tags/page'
import ProgressPage from './app/progress/page'
import SettingsPage from './app/settings/page'

const rootRoute = createRootRoute({
  component: RootLayout,
})

const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/',
  component: HomePage,
})

const loginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/login',
  component: LoginPage,
})

const registerRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/register',
  component: RegisterPage,
})

const perfumesRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/perfumes',
  component: PerfumesPage,
})

const perfumesNewRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/perfumes/new-perfume',
  component: NewPerfumePage,
})

const perfumesRecommendationsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/perfumes/recommendations',
  component: RecommendationsPage,
})

const perfumesAgentRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/perfumes/agent',
  component: ChatAgentPage,
})

export const perfumesIdRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/perfumes/$id',
  component: EditPerfumePage,
})

const statsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/stats',
  component: StatsPage,
})

const tagsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/tags',
  component: TagsPage,
})

const progressRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/progress',
  component: ProgressPage,
})

const settingsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/settings',
  component: SettingsPage,
})

const routeTree = rootRoute.addChildren([
  indexRoute,
  loginRoute,
  registerRoute,
  perfumesRoute,
  perfumesNewRoute,
  perfumesRecommendationsRoute,
  perfumesAgentRoute,
  perfumesIdRoute,
  statsRoute,
  tagsRoute,
  progressRoute,
  settingsRoute,
])

export const router = createRouter({ routeTree })

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router
  }
}
