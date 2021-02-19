import {
  colors,
  createStyles,
  IconButton,
  makeStyles,
  Theme,
  Typography,
} from "@material-ui/core";
import React, { useCallback } from "react";
import { useDropzone } from "react-dropzone";
import ClearIcon from "@material-ui/icons/Clear";
import CachedIcon from "@material-ui/icons/Cached";
import PracticeManagerApiClient from "../../PracticeManagerApiClient";

const client = new PracticeManagerApiClient(
  process.env.REACT_APP_API_URI_BASE as string
);

export interface UploadedItem {
  imageHref: string;
  thumbnailHref: string;
}

export interface PageItemProps {
  owner: string;
  scoreName: string;
  onUploaded?: (loadedItem: UploadedItem | undefined) => void;
  style?: React.CSSProperties;
  className?: string;
}

type PageUploadState = "none" | "uploading" | "success" | "error";

const PageItem = (props: PageItemProps) => {
  const classes = makeStyles((theme: Theme) =>
    createStyles({
      imageDropZoneRoot: {
        position: "relative",
        minWidth: "100%",
        minHeight: "100%",
      },
      imageDropZone: {
        position: "absolute",
        width: "100%",
        height: "100%",
        cursor: "pointer",
        textAlign: "center",
        justifyContent: "center",
        alignItems: "center",
        display: "flex",
        backgroundColor: colors.grey[200],
        "&:hover": {
          backgroundColor: colors.yellow[100],
        },
      },
      cardRoot: {
        width: "100%",
        height: "100%",
        position: "relative",
        display: "flex",
      },
      cardTextDiv: {
        position: "absolute",
        backgroundColor: "#FFFFFFEE",
      },
      cardImgDiv: {
        position: "absolute",
        width: "100%",
        height: "100%",
        display: "flex",
        justifyContent: "center",
      },
      cardImg: {
        objectFit: "contain",
      },
      cardButtonDiv: {
        position: "absolute",
        width: "100%",
        height: "100%",
        display: "flex",
        alignItems: "flex-end",
      },
      cardButtonContainer: {
        width: "100%",
        height: "50px",
        backgroundColor: "#FFFFFFEE",
        display: "flex",
        justifyContent: "flex-end",
      },
      cardButton: {},
    })
  )();

  const _onUploaded = props.onUploaded;
  const _style = props.style;
  const _className = props.className ?? "";

  const [state, setState] = React.useState<PageUploadState>("none");
  const [localUrl, setLocalUrl] = React.useState<string | undefined>(undefined);
  const [file, setFile] = React.useState<File | undefined>(undefined);
  const [remoteUrl, setRemoteUrl] = React.useState<string | undefined>(
    undefined
  );

  const _owner = props.owner;
  const _scoreName = props.scoreName;

  const upload = useCallback(
    async (file: File) => {
      console.log("upload start");

      if (!file) return;
      try {
        console.log("call api");
        const result = await client.uploadContent(file, _owner, _scoreName);

        setRemoteUrl(result.href);
        setState("success");

        if (_onUploaded)
          _onUploaded({
            imageHref: result.href,
            thumbnailHref: result.href,
          });
      } catch (err) {
        console.log(err);
        setRemoteUrl(undefined);
        setState("error");
      }

      console.log("upload end");
    },
    [_onUploaded, _owner, _scoreName]
  );

  const onUpdateDrop = useCallback(
    (acceptedFiles) => {
      acceptedFiles.forEach((f: File) => {
        const reader = new FileReader();

        reader.onabort = () => console.log("ファイルの読み込み中断");
        reader.onerror = () => console.log("ファイルの読み込みエラー");
        reader.onload = (e) => {
          // 正常に読み込みが完了した

          setLocalUrl(e.target?.result as string);
          setState("uploading");

          upload(f);
        };

        reader.readAsDataURL(f);

        setFile(f);
      });
    },
    [upload]
  );

  const updateDrop = useDropzone({ onDrop: onUpdateDrop });

  const clearOnClick = async () => {
    const href = remoteUrl;
    if (href) {
      try {
        console.log("call api");

        await client.deleteContent(href);
        if (_onUploaded) _onUploaded(undefined);
      } catch (err) {
        console.log(err);
        setState("error");
      }
    }

    setRemoteUrl(undefined);
    setState("none");

    if (_onUploaded) _onUploaded(undefined);
  };
  const reloadOnClick = async () => {
    if (!file) return;

    setState("uploading");
    await upload(file);
  };

  const content = (s: PageUploadState) => {
    switch (s) {
      case "none":
        return (
          <div className={classes.imageDropZoneRoot}>
            <div
              {...updateDrop.getRootProps()}
              className={classes.imageDropZone}
            >
              <input {...updateDrop.getInputProps()} />
              <Typography>画像をアップロードしてください</Typography>
            </div>
          </div>
        );
      case "uploading":
        return (
          <div className={classes.cardRoot}>
            <div className={classes.cardTextDiv}>
              <Typography variant="subtitle1" align="center">
                画像アップロード中
              </Typography>
              <Typography variant="caption" align="center" display="block">
                {file?.name}
              </Typography>
            </div>
          </div>
        );
      case "success":
        return (
          <div className={classes.cardRoot}>
            <div className={classes.cardImgDiv}>
              <img src={localUrl} alt={localUrl} className={classes.cardImg} />
            </div>
            <div className={classes.cardTextDiv}>
              <Typography variant="subtitle1" align="center">
                アップロード完了
              </Typography>
              <Typography variant="caption" align="center" display="block">
                {file?.name}
              </Typography>
            </div>
            <div className={classes.cardButtonDiv}>
              <div className={classes.cardButtonContainer}>
                <IconButton
                  aria-label="clear"
                  onClick={clearOnClick}
                  className={classes.cardButton}
                >
                  <ClearIcon />
                </IconButton>
              </div>
            </div>
          </div>
        );
      case "error":
        return (
          <div className={classes.cardRoot}>
            <div className={classes.cardImgDiv}>
              <img src={localUrl} alt={localUrl} className={classes.cardImg} />
            </div>
            <div className={classes.cardTextDiv}>
              <Typography variant="subtitle1" align="center">
                アップロードエラー
              </Typography>
              <Typography variant="caption" align="center" display="block">
                {file?.name}
              </Typography>
            </div>
            <div className={classes.cardButtonDiv}>
              <div className={classes.cardButtonContainer}>
                <IconButton
                  aria-label="clear"
                  onClick={clearOnClick}
                  className={classes.cardButton}
                >
                  <ClearIcon />
                </IconButton>
                <IconButton
                  aria-label="reload"
                  onClick={reloadOnClick}
                  className={classes.cardButton}
                >
                  <CachedIcon />
                </IconButton>
              </div>
            </div>
          </div>
        );
      default:
        return <></>;
    }
  };
  return (
    <div style={_style} className={_className}>
      {content(state)}
    </div>
  );
};

export default PageItem;
