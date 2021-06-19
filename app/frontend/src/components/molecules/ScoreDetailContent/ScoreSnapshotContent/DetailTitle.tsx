import { createStyles, makeStyles, Theme } from "@material-ui/core";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      width: "100%",
    },
    titleContainer: {
      width: "100%",
      "& > div": {
        height: "30px",
        display: "flex",
        alignItems: "center",
        "& p": {
          margin: 0,
        },
      },
      "& h2": {
        margin: 0,
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
        <div>
          <p>タイトル</p>
        </div>
        <h2>{_title}</h2>
      </div>
    </div>
  );
}
