import { CommonActions, TabActions } from '@react-navigation/native';
import { ROUTES } from '../constants';

/** Screens that live inside MainTabs (not on the root stack). */
const TAB_SCREENS = new Set<string>([
  ROUTES.Home,
  ROUTES.Tournament,
  ROUTES.Events,
  ROUTES.Wallet,
  ROUTES.Profile,
]);

type NavLike = any;

function findNavigatorWithRoute(navigation: NavLike, routeName: string): NavLike | null {
  let current: NavLike | undefined = navigation;
  for (let i = 0; i < 8 && current; i++) {
    const names = current.getState?.()?.routeNames as string[] | undefined;
    if (names?.includes(routeName)) return current;
    current = current.getParent?.();
  }
  return null;
}

/**
 * Safe navigate for nested tab + root stack.
 */
export function goTo(navigation: NavLike, route: string, params?: object) {
  if (TAB_SCREENS.has(route)) {
    const tabNav = findNavigatorWithRoute(navigation, route);
    if (tabNav?.dispatch) {
      tabNav.dispatch(TabActions.jumpTo(route, params));
      return;
    }
    if (tabNav) {
      if (params !== undefined) tabNav.navigate(route, params);
      else tabNav.navigate(route);
      return;
    }

    const stackNav = findNavigatorWithRoute(navigation, ROUTES.MainTabs);
    if (stackNav) {
      stackNav.navigate(ROUTES.MainTabs, { screen: route, params });
      return;
    }

    navigation.dispatch?.(
      CommonActions.navigate({
        name: ROUTES.MainTabs,
        params: { screen: route, params },
      }),
    );
    return;
  }

  if (params !== undefined) navigation.navigate(route, params);
  else navigation.navigate(route);
}

export function goHome(navigation: NavLike) {
  const root = findNavigatorWithRoute(navigation, ROUTES.WXOLobby) || navigation;
  if (root?.dispatch) {
    root.dispatch(
      CommonActions.reset({
        index: 0,
        routes: [{ name: ROUTES.WXOLobby }],
      }),
    );
    return;
  }
  goTo(navigation, ROUTES.Home);
}
