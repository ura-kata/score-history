import { createStyles, makeStyles, Theme } from "@material-ui/core";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      width: "100%",
    },
    titleContainer: {
      width: "100%",
      "& h2": {
        display: "flex",
        margin: 0,
        alignItems: "center",
      },
    },
  })
);

export interface DetailTitleProps {
  title?: string;
}

export default function DetailTitle(props: DetailTitleProps) {
  const _title = props.title;
  const classes = useStyles();

  return (
    <div className={classes.root}>
      <div className={classes.titleContainer}>
        <h2>
          <span>{_title}</span>
        </h2>
      </div>
    </div>
  );
}
