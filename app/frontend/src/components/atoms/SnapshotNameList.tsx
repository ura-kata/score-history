import {
  Button,
  createStyles,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  makeStyles,
  TextField,
  Theme,
} from "@material-ui/core";
import { useEffect, useState } from "react";
import { useHistory } from "react-router-dom";
import { scoreClientV2 } from "../../global";
import { ScoreSnapshotSummary } from "../../ScoreClientV2";
import CameraAltIcon from "@material-ui/icons/CameraAlt";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    toolbar: {
      width: "100%",
      height: "40px",
    },
    list: {
      padding: 0,
      "& li": {
        listStyle: "none",
      },
    },
    dialogContent: {
      width: "100px",
    },
  })
);

export interface SnapshotNameListProps {
  scoreId?: string;
}

export default function SnapshotNameList(props: SnapshotNameListProps) {
  const _scoreId = props.scoreId;

  const classes = useStyles();
  const [snapshotSummaries, setSnapshotSummaries] =
    useState<ScoreSnapshotSummary[]>();
  const [createSnapshotOpen, setCreateSnapshotOpen] = useState(false);
  const [newSnapshotName, setNewSnapshotName] = useState("");
  const [reload, setReload] = useState(0);

  const history = useHistory();
  useEffect(() => {
    if (_scoreId === undefined) return;

    const f = async () => {
      try {
        const result = await scoreClientV2.getSnapshotSummaries(_scoreId);
        setSnapshotSummaries(result);
      } catch (err) {
        console.log(err);
      }
    };

    f();
  }, [_scoreId, reload]);

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
    if (_scoreId === undefined) return;
    if (newSnapshotName === "") return;
    try {
      await scoreClientV2.createSnapshot(_scoreId, {
        name: newSnapshotName,
      });
      setNewSnapshotName("");
      setCreateSnapshotOpen(false);
      setReload(reload + 1);
    } catch (err) {
      console.log(err);
    }
  };
  return (
    <div style={{ width: "100%", height: "100%" }}>
      <div className={classes.toolbar}>
        <IconButton onClick={handleOnCreateClick}>
          <CameraAltIcon />
        </IconButton>
      </div>
      <ul className={classes.list}>
        {(snapshotSummaries ?? []).map((snap) => {
          const handleOnClick = () => {
            history.push(`/scores/${_scoreId}/snapshot/${snap.id}`);
          };
          return (
            <li key={snap.createAt.toString()}>
              <Button
                onClick={handleOnClick}
              >{`${snap.name} (${snap.createAt})`}</Button>
            </li>
          );
        })}
      </ul>
      <Dialog open={createSnapshotOpen} onClose={handleOnDialogClose}>
        <DialogTitle>{"スナップショットの作成"}</DialogTitle>
        <DialogContent>
          <form>
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
