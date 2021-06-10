import {
  Button,
  createStyles,
  Drawer,
  IconButton,
  makeStyles,
  Theme,
} from "@material-ui/core";
import React, { useCallback, useState } from "react";
import { useHistory, useParams } from "react-router-dom";
import useMeyScoreDetail from "../../hooks/scores/useMeyScoreDetail";
import CloseIcon from "@material-ui/icons/Close";
import ArrowBackIcon from "@material-ui/icons/ArrowBack";
import { useDropzone } from "react-dropzone";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    toolBar: {
      width: "100%",
      height: "50px",
    },
    editPageContainer: {
      width: "100%",
    },
    drawerContainer: {
      width: "50vw",
      display: "flex",
      flexFlow: "column",
    },
    drawerToolBar: {
      width: "100%",
      height: "50px",
      display: "flex",
      justifyContent: "flex-end",
    },
    drawerContent: {
      width: "100%",
    },
    imageDropZone: { width: "100%" },
    imageListContainer: { width: "100%", display: "flex", flexWrap: "wrap" },
    uploadImg: {
      height: "200px",
    },
  })
);

interface FileData {
  fileUrl: string;
  file: File;
}

interface ScorePageEditContentProps {}

export default function ScorePageEditContent(props: ScorePageEditContentProps) {
  const classes = useStyles();
  const { scoreId, pageId } =
    useParams<{ scoreId?: string; pageId?: string }>();
  const history = useHistory();
  const detail = useMeyScoreDetail({ scoreId, retryCount: 3 });

  const [openDrawer, setOpenDrawer] = useState(false);

  const handleOnDrawerClose = () => {
    setOpenDrawer(false);
  };
  const handleOnOpenClick = () => {
    setOpenDrawer(true);
  };
  const handleOnBackClick = () => {
    history.goBack();
  };

  const [fileDataList, setFileDataList] = React.useState([] as FileData[]);
  const onDrop = useCallback((acceptedFiles) => {
    setFileDataList([]);
    const loadedFileDataList: FileData[] = [];
    console.log("onDrop");

    acceptedFiles.forEach((f: File) => {
      console.log(f);
      const reader = new FileReader();

      reader.onabort = () => console.log("ファイルの読み込み中断");
      reader.onerror = () => console.log("ファイルの読み込みエラー");
      reader.onload = (e) => {
        // 正常に読み込みが完了した

        loadedFileDataList.push({
          fileUrl: e.target?.result as string,
          file: f,
        });
        setFileDataList([...loadedFileDataList]);
      };

      reader.readAsDataURL(f);
    });
  }, []);
  const uploadDrop = useDropzone({ onDrop: onDrop });

  return (
    <div style={{ width: "100%" }}>
      <div className={classes.toolBar}>
        <IconButton onClick={handleOnBackClick}>
          <ArrowBackIcon />
        </IconButton>
      </div>
      <div className={classes.editPageContainer}>
        <Button onClick={handleOnOpenClick}>open</Button>
      </div>
      <Drawer anchor="right" open={openDrawer} onClose={handleOnDrawerClose}>
        <div className={classes.drawerContainer}>
          <div className={classes.drawerToolBar}>
            <IconButton onClick={handleOnDrawerClose}>
              <CloseIcon />
            </IconButton>
          </div>
          <div className={classes.drawerContent}>
            <div {...uploadDrop.getRootProps()}>
              <input {...uploadDrop.getInputProps()} />
              <div className={classes.imageDropZone}>
                このエリアをクリックするするか画像ドロップしてアップロードしてください
              </div>
            </div>
          </div>
          <div className={classes.imageListContainer}>
            {fileDataList.map((fd, index) => (
              <img
                key={index}
                src={fd.fileUrl}
                alt={fd.file.name}
                className={classes.uploadImg}
              ></img>
            ))}
          </div>
        </div>
      </Drawer>
    </div>
  );
}
