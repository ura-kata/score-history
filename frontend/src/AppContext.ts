import { createContext, useReducer } from "react";
import { UserMe } from "./PracticeManagerApiClient";

export interface AppContextState{
  navigationOpen: boolean;
  userMe: UserMe | undefined;
}

export interface AppContextDispatchArgs{
  type: "openNavi"
    | "closeNavi"
    | "updateUserMe";
  payload?: any;
}

const defaultAppContextState: AppContextState = {
  navigationOpen: true,
  userMe: undefined,
}


export const useAppReducer = ()=>
  useReducer((state: AppContextState, action: AppContextDispatchArgs)=>{
    switch(action.type){
      case "openNavi": {
        return {...state, navigationOpen: true};
      }
      case "closeNavi": {
        return {...state, navigationOpen: false};
      }
      case "updateUserMe": {
        return {...state, userMe: action.payload};
      }
      default: {
        return state;
      }
    }
  }, defaultAppContextState);

export const AppContextDispatch = createContext((action: AppContextDispatchArgs) =>{});
export const AppContext = createContext(defaultAppContextState);
export default useAppReducer;
