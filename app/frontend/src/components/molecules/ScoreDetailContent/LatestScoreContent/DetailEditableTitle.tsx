import {
  createStyles,
  IconButton,
  makeStyles,
  TextField,
  Theme,
} from "@material-ui/core";
import React, { useState } from "react";
import EditIcon from "@material-ui/icons/Edit";
import DoneIcon from "@material-ui/icons/Done";
import CloseIcon from "@material-ui/icons/Close";
import { scoreClientV2 } from "../../../../global";

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
    titleContainerNone: {
      display: "none",
    },
    editorContainer: {
      width: "100%",
      display: "flex",
      justifyContent: "flex-start",
      justifyItems: "center",
      textAlign: "center",
      alignItems: "center",
      "& > *": {
        margin: "0 4px",
      },
      "& > div": {
        width: "calc(100% - (30px - 4px * 2) * 2)",
      },
    },
    editorContainerNone: { display: "none" },
  })
);

export interface DetailEditableTitleProps {
  id?: string;
  title?: string;
  onChangeTitle?: (newTitle: string) => void;
}

export default function DetailEditableTitle(props: DetailEditableTitleProps) {
  const _id = props.id;
  const _title = props.title;
  const _onChangeTitle = props.onChangeTitle;
  const classes = useStyles();

  const [edit, setEdit] = useState<Boolean>(false);
  const [newTitle, setNewTitle] = useState<string>("");
  const [updatedErrorText, setUpdatedErrorText] = useState<string>("");

  const handleOnChangeNewTitle = (
    event: React.ChangeEvent<HTMLInputElement>
  ) => {
    setNewTitle(event.target.value);
  };

  const handleOnClickEdit = () => {
    if (!_id) return;
    setNewTitle(_title ?? "");
    setEdit(true);
    setUpdatedErrorText("");
  };

  const handleOnClickDone = async () => {
    if (!_id) return;

    if (newTitle === _title) {
      setUpdatedErrorText("変更されていません");
      return;
    }
    try {
      await scoreClientV2.updateTitle(_id, { title: newTitle });
      setEdit(false);
      setUpdatedErrorText("");
      if (_onChangeTitle) {
        _onChangeTitle(newTitle);
      }
    } catch (err) {
      console.log(err);
      setUpdatedErrorText("楽譜の更新に失敗");
    }
  };

  const handleOnClickCancel = () => {
    setEdit(false);
    setUpdatedErrorText("");
  };

  return (
    <div className={classes.root}>
      <div
        className={edit ? classes.titleContainerNone : classes.titleContainer}
      >
        <div>
          <p>タイトル</p>
          <IconButton size="small" onClick={handleOnClickEdit}>
            <EditIcon />
          </IconButton>
        </div>

        <h2>{_title}</h2>
      </div>

      <div
        className={edit ? classes.editorContainer : classes.editorContainerNone}
      >
        <TextField
          variant="outlined"
          value={newTitle}
          onChange={handleOnChangeNewTitle}
          error={updatedErrorText ? true : false}
          helperText={updatedErrorText}
        />
        <IconButton size="small" onClick={handleOnClickDone}>
          <DoneIcon />
        </IconButton>
        <IconButton size="small" onClick={handleOnClickCancel}>
          <CloseIcon />
        </IconButton>
      </div>
    </div>
  );
}
