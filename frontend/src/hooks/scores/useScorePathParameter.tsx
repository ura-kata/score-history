import { useRouteMatch } from "react-router-dom";
import { HomeActionType } from "../../PathCreator";

export interface ScorePathParameter {
  owner?: string;
  scoreName?: string;
  action?: HomeActionType;
  pageIndex?: number;
}
export function useScorePathParameter(): ScorePathParameter {
  const urlMatch = useRouteMatch<{
    owner?: string;
    scoreName?: string;
    action?: string;
    pageIndex?: string;
  }>("/home/:owner?/:scoreName?/:action?/:pageIndex?");

  const owner = urlMatch?.params?.owner;
  const scoreName = urlMatch?.params?.scoreName;
  const action = urlMatch?.params?.action as HomeActionType | undefined;

  const pageIndexText = urlMatch?.params.pageIndex;
  const pageIndex =
    pageIndexText !== undefined ? parseInt(pageIndexText) : undefined;

  return {
    owner: owner,
    scoreName: scoreName,
    action: action,
    pageIndex: pageIndex,
  };
}
