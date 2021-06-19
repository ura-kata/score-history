import {
  Button,
  createStyles,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  IconButton,
  List,
  ListItem,
  makeStyles,
  TextField,
  Theme,
} from "@material-ui/core";
import { useEffect, useState } from "react";
import { useHistory, useParams } from "react-router-dom";
import { scoreClientV2 } from "../../../global";
import { ScoreSnapshotSummary } from "../../../ScoreClientV2";
import CameraAltIcon from "@material-ui/icons/CameraAlt";
import moment from "moment";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    toolbar: {
      width: "100%",
      height: "40px",
    },
    dialogContainer: {
      width: "100%",
      "& > div": {
        width: "100%",
        "& > div": {
          width: "100%",
        },
      },
    },
    buttonContainer: {
      width: "100%",
      "& > p": {
        width: "100%",
        margin: 0,
      },
    },
    name: {
      fontSize: "0.9em",
      fontWeight: 500,
    },
    at: {
      fontSize: "0.5em",
      fontWeight: 500,
    },
  })
);

function ToString(at: Date): string {
  const m = moment(at).local();
  return m.format("YYYY-MM-DD HH:mm:ss");
}

interface PathParameters {
  scoreId?: string;
  snapshotId?: string;
}

export interface SnapshotNameListProps {}

export default function SnapshotNameList(props: SnapshotNameListProps) {
  const classes = useStyles();

  const { scoreId, snapshotId } = useParams<PathParameters>();

  const [snapshotSummaries, setSnapshotSummaries] =
    useState<ScoreSnapshotSummary[]>();
  const [createSnapshotOpen, setCreateSnapshotOpen] = useState(false);
  const [newSnapshotName, setNewSnapshotName] = useState("");
  const [reload, setReload] = useState(0);

  const history = useHistory();
  useEffect(() => {
    if (scoreId === undefined) return;

    const f = async () => {
      try {
        const result = await scoreClientV2.getSnapshotSummaries(scoreId);
        setSnapshotSummaries(result);
      } catch (err) {
        console.log(err);
      }
    };

    f();
  }, [scoreId, reload]);

  const handleOnCreateClick = () => {
    setNewSnapshotName("");
    setCreateSnapshotOpen(true);
  };
  const handleOnDialogClose = () => {
    setCreateSnapshotOpen(false);
  };
  const handleOnNewSnapshotNameChange = (
    event: React.ChangeEvent<HTMLInputElement>
  ) => {
    setNewSnapshotName(event.target.value);
  };

  const handleOnCreateSnapshotClick = async () => {
    if (scoreId === undefined) return;
    if (newSnapshotName === "") return;
    try {
      await scoreClientV2.createSnapshot(scoreId, {
        name: newSnapshotName,
      });
      setNewSnapshotName("");
      setCreateSnapshotOpen(false);
      setReload(reload + 1);
    } catch (err) {
      console.log(err);
    }
  };

  const handleOnLatestClick = () => {
    history.push(`/scores/${scoreId}`);
  };
  return (
    <div style={{ width: "100%", height: "100%" }}>
      <div className={classes.toolbar}>
        <IconButton onClick={handleOnCreateClick}>
          <CameraAltIcon />
        </IconButton>
      </div>
      <List>
        <ListItem
          button
          onClick={handleOnLatestClick}
          selected={snapshotId ? false : true}
        >{`最新`}</ListItem>
        {(snapshotSummaries ?? []).map((snap, index) => {
          const handleOnClick = () => {
            history.push(`/scores/${scoreId}/snapshot/${snap.id}`);
          };
          const selected = snapshotId === snap.id;
          return (
            <>
              <Divider key={"div_" + index.toString()} />
              <ListItem
                key={snap.createAt.toString()}
                button
                onClick={handleOnClick}
                selected={selected}
              >
                <div className={classes.buttonContainer}>
                  <p className={classes.name}>{snap.name}</p>
                  <p className={classes.at}>{ToString(snap.createAt)}</p>
                </div>
              </ListItem>
            </>
          );
        })}
      </List>
      <Dialog open={createSnapshotOpen} onClose={handleOnDialogClose}>
        <DialogTitle>{"スナップショットの作成"}</DialogTitle>
        <DialogContent>
          <form className={classes.dialogContainer}>
            <div>
              <TextField
                value={newSnapshotName}
                onChange={handleOnNewSnapshotNameChange}
              ></TextField>
            </div>
          </form>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleOnCreateSnapshotClick}>作成</Button>
          <Button onClick={handleOnDialogClose}>キャンセル</Button>
        </DialogActions>
      </Dialog>
    </div>
  );
}
