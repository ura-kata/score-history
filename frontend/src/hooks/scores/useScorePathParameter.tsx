import { useRouteMatch } from "react-router-dom";
import { HomeActionType } from "../../PathCreator";

export interface ScorePathParameter {
  owner?: string;
  scoreName?: string;
  action?: HomeActionType;
  version?: string;
  pageIndex?: number;
}
export function useScorePathParameter(): ScorePathParameter {
  const urlMatch = useRouteMatch<{
    owner?: string;
    scoreName?: string;
    action?: string;
    version?: string;
    pageIndex?: string;
  }>("/home/:owner?/:scoreName?/:action?/:version?/:pageIndex?");

  const owner = urlMatch?.params?.owner;
  const scoreName = urlMatch?.params?.scoreName;
  const action = urlMatch?.params?.action as HomeActionType | undefined;

  const version = urlMatch?.params.version;
  const pageIndexText = urlMatch?.params.pageIndex;
  const pageIndex =
    pageIndexText !== undefined ? parseInt(pageIndexText) : undefined;

  return {
    owner: owner,
    scoreName: scoreName,
    action: action,
    version: version,
    pageIndex: pageIndex,
  };
}
