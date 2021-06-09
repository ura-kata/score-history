import { createStyles, IconButton, makeStyles, Theme } from "@material-ui/core";
import React from "react";
import ArrowBackIcon from "@material-ui/icons/ArrowBack";
import { useHistory } from "react-router";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      overflow: "hidden",
      display: "flex",
    },
    overflow: {
      height: "100vh",
      width: "100vw",
      position: "fixed",
      backgroundColor: "#000000FF",
    },
  })
);

export interface PageContentProps {
  scoreId: string;
}

export default function PageContent(props: PageContentProps) {
  const _scoreId = props.scoreId;
  const classes = useStyles();
  const history = useHistory();
  const handleOnClickBack = () => {
    history.push(`/scores/${_scoreId}`);
  };
  return (
    <div className={classes.root}>
      <div className={classes.overflow}>
        <IconButton onClick={handleOnClickBack}>
          <ArrowBackIcon />
        </IconButton>
      </div>
    </div>
  );
}
