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
    pageNo?: string;
  }>("/home/:owner?/:scoreName?/:action?/:version?/:pageNo?");

  let contentType: HomeContentType = "home";

  const owner = urlMatch?.params?.owner;
  const scoreName = urlMatch?.params?.scoreName;
  const scoreKey = owner && scoreName ? `${owner}/${scoreName}` : undefined;
  let score: undefined | ScoreV2Latest = undefined;

  if (scoreKey) {
    score = scoreSet[scoreKey];

    if (score) {
      contentType = "detail";
    }
  }

  let version: undefined | string = undefined;
  if (urlMatch) {
    version = urlMatch.params.version;
    if (version !== undefined) {
      contentType = "version";
    }
  }

  let pageNo: undefined | number = undefined;
  if (urlMatch) {
    const pageNoText = urlMatch.params.pageNo;
    pageNo = pageNoText !== undefined ? parseInt(pageNoText) : undefined;
    if (pageNo) {
      contentType = "page";
    }
  }

  const handleOnCardClick = (owner: string, scoreName: string) => {
    history.push(`/home/${owner}/${scoreName}/`);
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
            pageIndex={pageNo}
          />
        );
      }
      default:
        return <></>;
    }
  })(contentType);

  const breadcrumbList = [
    <Button key={0} component={Link} to="/">
      Home
    </Button>,
  ];
  if (score) {
    breadcrumbList.push(
      <Button
        key={breadcrumbList.length}
        component={Link}
        to={`/home/${scoreKey}/`}
      >
        <Typography> {`${scoreName} (${owner})`}</Typography>
      </Button>
    );

    if (version) {
      breadcrumbList.push(
        <Button
          key={breadcrumbList.length}
          component={Link}
          to={`/home/${scoreKey}/version/${version}`}
        >
          version {version}
        </Button>
      );
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
