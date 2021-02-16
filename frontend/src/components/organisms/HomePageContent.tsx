import React from "react";
import { useHistory } from "react-router-dom";
import { ScorePage, ScoreProperty, ScoreSummarySet } from "../../ScoreClient";
import { PathCreator } from "../pages/HomePage";
import EditScorePropertyContent from "./EditScorePropertyContent";
import ScoreDetailContent from "./ScoreDetailContent";
import SocreListContent from "./SocreListContent";
import UpdatePageContent from "./UpdatePageContent";

export type HomeActionType = "edit" | "version" | "edit-page";
export interface HomeContentProps {
  owner?: string;
  scoreName?: string;
  type?: HomeActionType;
  selectedVersion?: string;
  selectedPageIndex?: number;
  pathCreator: PathCreator;
}

const HomeContent = (props: HomeContentProps) => {
  const _owner = props.owner;
  const _scorename = props.scoreName;
  const _type = props.type;
  const _selectedVersion = props.selectedVersion;
  const _selectedPageIndex = props.selectedPageIndex;

  const _pathCreator = props.pathCreator;

  if (_owner && _scorename) {
    if ("edit" === _type) {
      return (
        <EditScorePropertyContent
          owner={_owner}
          scoreName={_scorename}
          pathCreator={_pathCreator}
        />
      );
    }

    if ("edit-page" === _type) {
      return (
        <UpdatePageContent
          owner={_owner}
          scoreName={_scorename}
          pathCreator={_pathCreator}
        />
      );
    }

    return (
      <ScoreDetailContent
        owner={_owner}
        scoreName={_scorename}
        selectedVersion={_selectedVersion}
        selectedPageIndex={_selectedPageIndex}
        pathCreator={_pathCreator}
      />
    );
  }

  return <SocreListContent pathCreator={_pathCreator} />;
};

export default HomeContent;
