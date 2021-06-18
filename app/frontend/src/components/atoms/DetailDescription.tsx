import { createStyles, makeStyles, Theme } from "@material-ui/core";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      width: "100%",
    },
    descriptionContainer: {
      width: "100%",
    },
    description: {
      width: "auto",
    },
    descP: {},
  })
);

export interface DetailDescriptionProps {
  description?: string;
}

export default function DetailDescription(props: DetailDescriptionProps) {
  const _description = props.description;
  const classes = useStyles();

  return (
    <div className={classes.root}>
      <div className={classes.descriptionContainer}>
        <div className={classes.description}>
          {_description?.split("\n").map((p, index) => (
            <p key={index} className={classes.descP}>
              {p}
            </p>
          ))}
        </div>
      </div>
    </div>
  );
}
