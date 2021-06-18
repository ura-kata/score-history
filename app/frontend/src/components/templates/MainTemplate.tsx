import { createStyles, makeStyles, Theme } from "@material-ui/core";
import Copyright from "../molecules/MainTemplate/Copyright";
import React from "react";
import MainAppBar from "../molecules/MainTemplate/MainAppBar";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      width: "100%",
    },
    appBar: {
      height: "50px",
      width: "100%",
      position: "sticky",
    },
    contentContainer: {
      width: "100%",
      minHeight: "calc(100vh - 100px)",
    },
    content: {
      width: "100%",
    },
    footer: {
      height: "50px",
      width: "100%",
      display: "flex",
      justifyContent: "center",
      alignItems: "center",
    },
  })
);

export interface MainTemplateProps {
  children: React.ReactNode;
}

export default function MainTemplate(props: MainTemplateProps) {
  var _children = props.children;

  var classes = useStyles();

  return (
    <div className={classes.root}>
      <div className={classes.appBar}>
        <MainAppBar />
      </div>
      <div className={classes.contentContainer}>
        <div className={classes.content}>{_children}</div>
      </div>
      <footer className={classes.footer}>
        <Copyright />
      </footer>
    </div>
  );
}
