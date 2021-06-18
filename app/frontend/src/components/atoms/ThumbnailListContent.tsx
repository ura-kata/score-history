import { createStyles, makeStyles, Theme } from "@material-ui/core";
import { ScorePage } from "../../ScoreClientV2";
import { ThumbnailItem } from "./ThumbnailItem";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      height: "100%",
    },
  })
);

export interface ThumbnailListContentProps {
  ownerId?: string;
  scoreId?: string;
  pages?: ScorePage[];
}

export function ThumbnailListContent(props: ThumbnailListContentProps) {
  const ownerId = props.ownerId;
  const scoreId = props.scoreId;
  const _pages = props.pages ?? [];
  const classes = useStyles();
  return (
    <div className={classes.root}>
      {_pages.map((p) =>
        ownerId && scoreId ? (
          <ThumbnailItem
            ownerId={ownerId}
            scoreId={scoreId}
            key={p.id}
            page={p}
          />
        ) : (
          <></>
        )
      )}
    </div>
  );
}
