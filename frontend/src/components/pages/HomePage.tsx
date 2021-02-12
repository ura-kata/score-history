import React, { useEffect, useState } from "react";
import GenericTemplate from "../templates/GenericTemplate";
import {
  createStyles,
  makeStyles,
  Theme,
  colors,
  Typography,
  Button,
  Breadcrumbs,
  withStyles,
} from "@material-ui/core";
import {
  ScoreV2Latest,
  ScoreV2LatestSet,
  ScoreV2PageObject,
  ScoreV2VersionObject,
  ScoreV2VersionSet,
} from "../../PracticeManagerApiClient";
import { Link, useHistory, useRouteMatch } from "react-router-dom";
import ScoreDetailContent from "../organisms/ScoreDetailContent";
import ScoreVersionDetailContent from "../organisms/ScoreVersionDetailContent";
import SocreListContent from "../organisms/SocreListContent";
import { scoreClient } from "../../global";

// ------------------------------------------------------------------------------------------

class PathCreator {
  getHomePath(): string {
    return `/`;
  }
  getDetailPath(owner: string, scoreName: string): string {
    return `/home/${owner}/${scoreName}/`;
  }
  getUpdatePath(owner: string, scoreName: string): string {
    return `/home/${owner}/${scoreName}/update/`;
  }
  getVersionPath(owner: string, scoreName: string, version: string): string {
    return `/home/${owner}/${scoreName}/version/${version}/`;
  }
  getPagePath(
    owner: string,
    scoreName: string,
    version: string,
    pageIndex: number
  ): string {
    return `/home/${owner}/${scoreName}/version/${version}/${pageIndex}/`;
  }
}

// ------------------------------------------------------------------------------------------

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    scoreCard: {
      width: "300px",
      margin: theme.spacing(1),
    },
    scoreCardName: {
      color: colors.grey[400],
    },
    scoreCardContainer: {
      margin: theme.spacing(3, 0, 0),
    },
    breadcrumbsLink: {
      textDecoration: "none",
      textTransform: "none",
    },
  })
);

type HomeContentType = "home" | "detail" | "version" | "page";

const HomePage = () => {
  const classes = useStyles();

  const [scoreSet, setScoreSet] = useState<ScoreV2LatestSet>({});
  const [versionObject, setVersionObject] = useState<ScoreV2VersionObject>();
  const [pages, setPages] = useState<ScoreV2PageObject[]>([]);

  const history = useHistory();
  const urlMatch = useRouteMatch<{
    owner?: string;
    scoreName?: string;
    action?: string;
    version?: string;
    pageIndex?: string;
  }>("/home/:owner?/:scoreName?/:action?/:version?/:pageIndex?");

  const pathCreator = new PathCreator();

  const owner = urlMatch?.params?.owner;
  const scoreName = urlMatch?.params?.scoreName;
  const scoreKey = owner && scoreName ? `${owner}/${scoreName}` : undefined;
  const score =
    owner && scoreName ? scoreSet[`${owner}/${scoreName}`] : undefined;

  const version = urlMatch?.params.version;
  const pageIndexText = urlMatch?.params.pageIndex;
  const pageIndex =
    pageIndexText !== undefined ? parseInt(pageIndexText) : undefined;

  const contentType: HomeContentType = (() => {
    if (pageIndex !== undefined) {
      return "page";
    }

    if (version !== undefined) {
      return "version";
    }

    if (score) {
      return "detail";
    }
    return "home";
  })();

  const handleOnCardClick = (owner: string, scoreName: string) => {
    history.push(pathCreator.getDetailPath(owner, scoreName));
  };

  const [versionSet, setVersionSet] = useState<ScoreV2VersionSet>({});

  const loadScoreSet = async () => {
    try {
      const scoreSet = await scoreClient.getScores();
      setScoreSet(scoreSet);
    } catch (err) {
      console.log(err);
    }
  };

  useEffect(() => {
    loadScoreSet();
  }, []);

  const loadVersionSet = async (owner: string, scoreName: string) => {
    try {
      const versionSet = await scoreClient.getVersions(owner, scoreName);
      setVersionSet(versionSet);
    } catch (err) {
      console.log(err);
    }
  };

  useEffect(() => {
    if (!owner) return;
    if (!scoreName) return;
    loadVersionSet(owner, scoreName);
  }, [owner, scoreName]);

  const loadVersion = async (
    owner: string,
    scoreName: string,
    hash: string
  ) => {
    try {
      const versionObject = await scoreClient.getVersion(
        owner,
        scoreName,
        hash
      );
      setVersionObject(versionObject);
    } catch (err) {
      console.log(err);
    }
  };

  useEffect(() => {
    if (!owner) return;
    if (!scoreName) return;
    if (!version) return;
    if (!versionSet) return;

    var hash = versionSet[version];
    if (!hash) return;

    loadVersion(owner, scoreName, hash);
  }, [owner, scoreName, version, versionSet]);

  const loadPages = async (
    owner: string,
    scoreName: string,
    version: ScoreV2VersionObject
  ) => {
    try {
      const pages = await scoreClient.getPages(owner, scoreName, version);
      setPages(pages);
    } catch (err) {
      console.log(err);
    }
  };
  useEffect(() => {
    if (!owner) return;
    if (!scoreName) return;
    if (!versionObject) return;
    loadPages(owner, scoreName, versionObject);
  }, [owner, scoreName, versionObject]);

  const handleOnRefreshClick = async () => {
    await loadScoreSet();
  };

  const content = ((type: HomeContentType) => {
    switch (type) {
      case "home": {
        return (
          <SocreListContent
            key={type}
            scoreSet={scoreSet}
            onCardClick={handleOnCardClick}
            onRefreshClick={handleOnRefreshClick}
          />
        );
      }
      case "detail": {
        return (
          <ScoreDetailContent
            key={type}
            score={score}
            owner={owner}
            scoreName={scoreName}
            versionSet={versionSet}
          />
        );
      }
      case "version": {
        return (
          <ScoreVersionDetailContent
            key={type}
            owner={owner}
            scoreName={scoreName}
            version={version}
            versionObject={versionObject}
            pages={pages}
          />
        );
      }
      case "page": {
        return (
          <ScoreVersionDetailContent
            key={type}
            owner={owner}
            scoreName={scoreName}
            version={version}
            versionObject={versionObject}
            pages={pages}
            pageIndex={pageIndex}
          />
        );
      }
      default:
        return <></>;
    }
  })(contentType);

  const breadcrumbList = [
    <Button
      key={0}
      className={classes.breadcrumbsLink}
      component={Link}
      to={pathCreator.getHomePath()}
    >
      Home
    </Button>,
  ];
  if (owner && scoreName) {
    if (score) {
      breadcrumbList.push(
        <Button
          key={breadcrumbList.length}
          className={classes.breadcrumbsLink}
          component={Link}
          to={pathCreator.getDetailPath(owner, scoreName)}
        >
          <Typography> {`${scoreName} (${owner})`}</Typography>
        </Button>
      );

      if (version) {
        breadcrumbList.push(
          <Button
            key={breadcrumbList.length}
            className={classes.breadcrumbsLink}
            component={Link}
            to={pathCreator.getVersionPath(owner, scoreName, version)}
          >
            version {version}
          </Button>
        );
      }
    }
  }

  return (
    <GenericTemplate>
      <Breadcrumbs>{breadcrumbList}</Breadcrumbs>

      {content}
    </GenericTemplate>
  );
};

export default HomePage;
