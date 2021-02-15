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
  scoreSummarySet: ScoreSummarySet;
  owner?: string;
  scoreName?: string;
  type?: HomeActionType;
  property: ScoreProperty;
  versions: string[];
  selectedVersion?: string;
  pages: ScorePage[];
  selectedPageIndex?: number;
  onLoadedScoreSummarySet?: (scoreSummarySet: ScoreSummarySet) => void;
  onLoadedVersions?: (versions: string[]) => void;
  onLoadedPages?: (versions: ScorePage[]) => void;
  pathCreator: PathCreator;
}

const HomeContent = (props: HomeContentProps) => {
  const _scoreSummarySet = props.scoreSummarySet;
  const _owner = props.owner;
  const _scorename = props.scoreName;
  const _type = props.type;
  const _property = props.property;
  const _versions = props.versions;
  const _selectedVersion = props.selectedVersion;
  const _pages = props.pages;
  const _selectedPageIndex = props.selectedPageIndex;

  const _onLoadedScoreSummarySet = props.onLoadedScoreSummarySet;
  const _onLoadedVersions = props.onLoadedVersions;
  const _onLoadedPages = props.onLoadedPages;
  const _pathCreator = props.pathCreator;

  const handleOnLoadedScoreData = (
    scoreSet: ScoreSummarySet,
    versions: string[]
  ) => {
    if (_onLoadedScoreSummarySet) {
      _onLoadedScoreSummarySet(scoreSet);
    }
    if (_onLoadedVersions) {
      _onLoadedVersions(versions);
    }
  };

  if (_owner && _scorename) {
    if ("edit" === _type) {
      return (
        <EditScorePropertyContent
          owner={_owner}
          scoreName={_scorename}
          title={_property.title}
          description={_property.description}
          pathCreator={_pathCreator}
          onLoadedScoreData={handleOnLoadedScoreData}
        />
      );
    }

    if ("edit-page" === _type) {
      return (
        <UpdatePageContent
          owner={_owner}
          scoreName={_scorename}
          pages={_pages}
          pathCreator={_pathCreator}
        />
      );
    }

    return (
      <ScoreDetailContent
        owner={_owner}
        scoreName={_scorename}
        property={_property}
        versions={_versions}
        selectedVersion={_selectedVersion}
        pages={_pages}
        selectedPageIndex={_selectedPageIndex}
        pathCreator={_pathCreator}
      />
    );
  }

  return (
    <SocreListContent
      scoreSet={_scoreSummarySet}
      onLoadedScoreSummarySet={_onLoadedScoreSummarySet}
      pathCreator={_pathCreator}
    />
  );
};

export default HomeContent;
