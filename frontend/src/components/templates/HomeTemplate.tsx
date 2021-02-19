import {
  Breadcrumbs,
  Button,
  colors,
  createStyles,
  Grid,
  makeStyles,
  Theme,
  Typography,
} from "@material-ui/core";
import React from "react";
import { Link } from "react-router-dom";
import { useScorePathParameter } from "../../hooks/scores/useScorePathParameter";
import PathCreator from "../../PathCreator";
import GenericTemplate from "./GenericTemplate";

// ---------------------------------------------------------------------------

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

export interface HomeTemplateProps {
  children: React.ReactNode;
}

export default function HomeTemplate(props: HomeTemplateProps) {
  const _children = props.children;

  const classes = useStyles();
  const pathParam = useScorePathParameter();
  const owner = pathParam.owner;
  const scoreName = pathParam.scoreName;
  const version = pathParam.version;

  const pathCreator = new PathCreator();

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
          <Breadcrumbs>{breadcrumbList}</Breadcrumbs>
        </Grid>
        <Grid item xs={12}>
          {_children}
        </Grid>
      </Grid>
    </GenericTemplate>
  );
}
