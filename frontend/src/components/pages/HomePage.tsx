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
  Grid,
} from "@material-ui/core";
import { Link, useRouteMatch } from "react-router-dom";
import { scoreClient } from "../../global";
import { Alert } from "@material-ui/lab";
import { ScorePage, ScoreSummarySet } from "../../ScoreClient";
import HomeContent, { HomeActionType } from "../organisms/HomePageContent";

// ------------------------------------------------------------------------------------------

export class PathCreator {
  getHomePath(): string {
    return `/`;
  }
  getDetailPath(owner: string, scoreName: string): string {
    return `/home/${owner}/${scoreName}/`;
  }
  getEditPropertyPath(owner: string, scoreName: string): string {
    const action: HomeActionType = "edit";
    return `/home/${owner}/${scoreName}/${action}/`;
  }
  getEditPagePath(owner: string, scoreName: string): string {
    const action: HomeActionType = "edit-page";
    return `/home/${owner}/${scoreName}/${action}/`;
  }
  getVersionPath(owner: string, scoreName: string, version: string): string {
    const action: HomeActionType = "version";
    return `/home/${owner}/${scoreName}/${action}/${version}/`;
  }
  getPagePath(
    owner: string,
    scoreName: string,
    version: string,
    pageIndex: number
  ): string {
    const action: HomeActionType = "version";
    return `/home/${owner}/${scoreName}/${action}/${version}/${pageIndex}/`;
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

const HomePage = () => {
  const classes = useStyles();

  const [scoreSet, setScoreSet] = useState<ScoreSummarySet>({});
  const [versions, setVersions] = useState<string[]>([]);
  const [pages, setPages] = useState<ScorePage[]>([]);

  const [loadScoreSetError, setLoadScoreSetError] = useState<string>();
  const [loadVersionSetError, setLoadVersionsError] = useState<string>();
  const [loadPagesError, setLoadPagesError] = useState<string>();

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
  const action = urlMatch?.params?.action as HomeActionType | undefined;

  const version = urlMatch?.params.version ?? (versions ?? []).slice(-1)[0];
  const pageIndexText = urlMatch?.params.pageIndex;
  const pageIndex =
    pageIndexText !== undefined ? parseInt(pageIndexText) : undefined;

  const score =
    owner && scoreName ? scoreSet[`${owner}/${scoreName}`] : undefined;
  const property = score?.property ?? {};

  const loadScoreSet = async () => {
    try {
      const scoreSummarys = await scoreClient.getScores();
      setScoreSet(scoreSummarys);
      setLoadScoreSetError(undefined);
    } catch (err) {
      setLoadScoreSetError(`楽譜の一覧取得に失敗しました`);
      console.log(err);
    }
  };

  const loadVersionSet = async (owner: string, scoreName: string) => {
    try {
      const versions = await scoreClient.getVersions(owner, scoreName);
      setVersions(versions);
      setLoadVersionsError(undefined);
    } catch (err) {
      setLoadVersionsError(`楽譜のバージョン一覧の取得に失敗しました`);
      console.log(err);
    }
  };

  const loadPages = async (
    owner: string,
    scoreName: string,
    version: string
  ) => {
    try {
      const pages = await scoreClient.getPages(owner, scoreName, version);
      setPages(pages);
      setLoadPagesError(undefined);
    } catch (err) {
      setLoadPagesError(`ページの情報取得に失敗しました`);
      console.log(err);
    }
  };

  useEffect(() => {
    loadScoreSet();
  }, []);

  useEffect(() => {
    if (!owner) return;
    if (!scoreName) return;
    loadVersionSet(owner, scoreName);
  }, [owner, scoreName]);

  useEffect(() => {
    if (!owner) return;
    if (!scoreName) return;
    if (!version) return;
    loadPages(owner, scoreName, version);
  }, [owner, scoreName, version]);

  const handleOnLoadedScoreSummarySet = (scoreSet: ScoreSummarySet) => {
    setScoreSet(scoreSet);
  };

  const handleOnLoadedVersions = (versions: string[]) => {
    setVersions(versions.slice());
  };

  const handleOnLoadedPages = (pages: ScorePage[]) => {
    setPages(pages.slice());
  };

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

  return (
    <GenericTemplate>
      <Grid container>
        <Grid item xs={12}>
          {loadScoreSetError ? (
            <Alert severity="error">{loadScoreSetError}</Alert>
          ) : (
            <></>
          )}
        </Grid>
        <Grid item xs={12}>
          {loadVersionSetError ? (
            <Alert severity="error">{loadVersionSetError}</Alert>
          ) : (
            <></>
          )}
        </Grid>
        <Grid item xs={12}>
          {loadPagesError ? (
            <Alert severity="error">{loadPagesError}</Alert>
          ) : (
            <></>
          )}
        </Grid>
        <Grid item xs={12}>
          <Breadcrumbs>{breadcrumbList}</Breadcrumbs>
        </Grid>
        <Grid item xs={12}>
          <HomeContent
            scoreSummarySet={scoreSet}
            owner={owner}
            scoreName={scoreName}
            property={property}
            versions={versions}
            selectedVersion={version}
            pages={pages}
            selectedPageIndex={pageIndex}
            onLoadedScoreSummarySet={handleOnLoadedScoreSummarySet}
            onLoadedVersions={handleOnLoadedVersions}
            onLoadedPages={handleOnLoadedPages}
            pathCreator={pathCreator}
            type={action}
          />
        </Grid>
      </Grid>
    </GenericTemplate>
  );
};

export default HomePage;
