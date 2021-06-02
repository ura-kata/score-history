import { createContext, useReducer } from "react";
import { UserMe } from "./PracticeManagerApiClient";
import { UserData } from "./UserClient";

export interface AppContextState {
  navigationOpen: boolean;
  userData: UserData | undefined;
  userMe: UserData | undefined;
}

export interface AppContextDispatchArgs {
  type: "openNavi" | "closeNavi" | "updateUserData";
  payload?: any;
}

const defaultAppContextState: AppContextState = {
  navigationOpen: true,
  userData: undefined,
  userMe: undefined,
};

export const useAppReducer = () =>
  useReducer((state: AppContextState, action: AppContextDispatchArgs) => {
    switch (action.type) {
      case "openNavi": {
        return { ...state, navigationOpen: true };
      }
      case "closeNavi": {
        return { ...state, navigationOpen: false };
      }
      case "updateUserData": {
        return { ...state, userData: action.payload };
      }
      default: {
        return state;
      }
    }
  }, defaultAppContextState);

export const AppContextDispatch = createContext(
  (action: AppContextDispatchArgs) => {}
);
export const AppContext = createContext(defaultAppContextState);
export default useAppReducer;
