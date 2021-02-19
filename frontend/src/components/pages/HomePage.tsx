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
import HomeContent from "../organisms/HomePageContent";
import PathCreator, { HomeActionType } from "../../PathCreator";

// ------------------------------------------------------------------------------------------

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

  const [loadScoreSetError, setLoadScoreSetError] = useState<string>();

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

  const version = urlMatch?.params.version;
  const pageIndexText = urlMatch?.params.pageIndex;
  const pageIndex =
    pageIndexText !== undefined ? parseInt(pageIndexText) : undefined;

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
          <Breadcrumbs>{breadcrumbList}</Breadcrumbs>
        </Grid>
        <Grid item xs={12}>
          <HomeContent
            owner={owner}
            scoreName={scoreName}
            selectedVersion={version}
            selectedPageIndex={pageIndex}
            pathCreator={pathCreator}
            type={action}
          />
        </Grid>
      </Grid>
    </GenericTemplate>
  );
};

export default HomePage;
