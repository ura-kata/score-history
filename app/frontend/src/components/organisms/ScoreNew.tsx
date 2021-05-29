import {
  Button,
  createStyles,
  IconButton,
  makeStyles,
  TextField,
  Theme,
} from "@material-ui/core";
import { Add, ArrowBack } from "@material-ui/icons";
import React, { useState } from "react";
import { useHistory } from "react-router";
import { scoreClientV2 } from "../../global";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      width: "100%",
    },
    controlBar: {
      width: "100%",
      height: "40px",
    },
    controlButton: {
      margin: "5px",
    },
    fieldRoot: {
      width: "100%",
    },
    field: {
      width: "calc(100% - 20px)",
      margin: "10px",
    },
  })
);

export interface ScoreNewProps {}

/** 楽譜を新しく作成するためのコンポーネント */
export default function ScoreNew(props: ScoreNewProps) {
  const classes = useStyles();

  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const history = useHistory();

  const handleBack = () => {
    history.push("/");
  };

  const handleCreate = async () => {
    try {
      if (!title) {
        setErrorMessage("タイトルを入力してください");
        return;
      }
      await scoreClientV2.create({ title: title, description: description });
      history.push("/");
    } catch (err) {}
  };

  const handleChangeTitle =
    (type: "title" | "description") =>
    (event: React.ChangeEvent<HTMLInputElement>) => {
      const value = event.target.value;
      switch (type) {
        case "title": {
          setTitle(value);
          break;
        }
        case "description": {
          setDescription(value);
          break;
        }
      }
    };

  return (
    <div className={classes.root}>
      <div className={classes.controlBar}>
        <Button
          variant="contained"
          color="inherit"
          size="small"
          className={classes.controlButton}
          startIcon={<ArrowBack />}
          onClick={handleBack}
        >
          戻る
        </Button>
        <Button
          variant="contained"
          color="primary"
          size="small"
          className={classes.controlButton}
          startIcon={<Add />}
          onClick={handleCreate}
        >
          作成
        </Button>
      </div>
      <div className={classes.fieldRoot}>
        <form>
          <div>
            <TextField
              className={classes.field}
              variant="outlined"
              label="タイトル"
              required
              value={title}
              onChange={handleChangeTitle("title")}
            ></TextField>
            <TextField
              className={classes.field}
              variant="outlined"
              label="説明"
              multiline
              value={description}
              onChange={handleChangeTitle("description")}
            ></TextField>
          </div>
        </form>
      </div>
    </div>
  );
}
