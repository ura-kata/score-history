import { Button, colors, createStyles, Dialog, DialogActions, DialogContent, DialogTitle, Grid, GridList, GridListTile, GridListTileBar, makeStyles, TextField, Theme } from "@material-ui/core";
import React, { useCallback } from "react";
import { useDropzone } from "react-dropzone";
import PracticeManagerApiClient from "../../PracticeManagerApiClient";

const client = new PracticeManagerApiClient("http://localhost:5000/");

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    img:{
      height: "50vh"
    },
    imageDropZoneRoot: {
      margin:"20px",
    },
    imageDropZone:{
      width: "100%",
      height: "50px",
      cursor: "pointer",
      backgroundColor: colors.grey[200],
      textAlign: "center",
      '&:hover': {
        backgroundColor: colors.yellow[100],
      }
    },
    imageList:{
      backgroundColor: colors.grey[100],
    },
    table: {
      height: 400,
      width: '100%'
    }
  })
);

export interface UploadDialogProps{
  onUploaded?: ((event: {}) => void) | undefined;
  onCanceled?: ((event: {}) => void) | undefined;
  open: boolean;

}

interface FileData{
  fileUrl: string;
  file: File;
}

const UploadDialog = (props: UploadDialogProps)=>{

  const onUploaded = props.onUploaded;
  const onCanceled = props.onCanceled;
  const open = props.open;

  const classes = useStyles();

  const [uploadScoreName, setUploadScoreName] = React.useState("");
  const [uploadScoreTitle, setUploadScoreTitle] = React.useState("");
  const [uploadScoreDescription, setUploadScoreDescription] = React.useState("");
  const [fileDataList, setFileDataList] = React.useState([] as FileData[]);

  const handlerUploadScoreName = useCallback(async (event)=>{
    setUploadScoreName(event.target.value);
  }, []);
  const handlerUploadScoreTitle = useCallback(async (event)=>{
    setUploadScoreTitle(event.target.value);
  }, []);
  const handlerUploadScoreDescription = useCallback(async (event)=>{
    setUploadScoreDescription(event.target.value);
  }, []);

  const onDrop = useCallback((acceptedFiles) => {
    setFileDataList([])
    const loadedFileDataList: FileData[] = [];
    console.log("onDrop");

    acceptedFiles.forEach((f: File) => {
      console.log(f);
      const reader = new FileReader();

      reader.onabort = () => console.log('ファイルの読み込み中断');
      reader.onerror = () => console.log('ファイルの読み込みエラー');
      reader.onload = (e) => {
        // 正常に読み込みが完了した

          loadedFileDataList.push({
            fileUrl: e.target?.result as string,
            file: f
          });
          setFileDataList([...loadedFileDataList]);

      };

      reader.readAsDataURL(f);
    })

  },[]);
  const uploadDrop = useDropzone({onDrop:onDrop});

  const handleUploadDialogClose = useCallback(async () => {
    if(onCanceled){
      onCanceled({});
    }
  },[onCanceled]);

  const handlerUpload = useCallback(async ()=>{
    try{
      await client.createScore({
        name: uploadScoreName,
        title: uploadScoreTitle,
        description: uploadScoreDescription
      });
      await client.createVersion(uploadScoreName, fileDataList.map(x=>x.file));
      alert('画像をアップロードしました');
      handleUploadDialogClose();

      setUploadScoreName("");
      setUploadScoreTitle("");
      setUploadScoreDescription("");
      setFileDataList([]);
    } catch(err) {
      alert('ファイルのアップロードに失敗しました');
      return;
    }

    if(onUploaded){
      onUploaded({});
    }
  },[onUploaded, uploadScoreName, uploadScoreTitle, uploadScoreDescription, fileDataList, handleUploadDialogClose]);

  const uploadDialog = (
    <Dialog
      open={open}
      onClose={handleUploadDialogClose}
      scroll={'paper'}
    >
      <DialogTitle>スコアをアップロードします</DialogTitle>
      <DialogContent dividers={true}>
        <Grid container spacing={3}>
          <Grid item xs={12}>
            <TextField value={uploadScoreName} onChange={handlerUploadScoreName} label={'スコアの名前'}></TextField>
          </Grid>
          <Grid item xs={12}>
            <TextField value={uploadScoreTitle} onChange={handlerUploadScoreTitle} label={'スコアのタイトル'}></TextField>
          </Grid>
          <Grid item xs={12}>
            <TextField value={uploadScoreDescription} onChange={handlerUploadScoreDescription} label={'スコアの説明'}></TextField>
          </Grid>
          <Grid item xs={12}>
            <div className={classes.imageDropZoneRoot}>
              <div {...uploadDrop.getRootProps()}>
                <input {...uploadDrop.getInputProps()} />
                <div className={classes.imageDropZone}>
                  このエリアをクリックするするか画像ドロップしてアップロードしてください
                </div>
              </div>
            </div>
          </Grid>
          <Grid item xs={12}>
            <GridList className={classes.imageList}>
              {
                fileDataList.map((fd, i) => (
                  <GridListTile key={'image' + i.toString()}>
                    <img src={fd.fileUrl} className={classes.img} alt={fd.file.name}></img>
                    <GridListTileBar title={fd.file.name}/>
                  </GridListTile>
                ))
              }
            </GridList>
          </Grid>
        </Grid>

      </DialogContent>
      <DialogActions>
        <Button variant="outlined" color="primary" onClick={handlerUpload}>アップロード</Button>
        <Button variant="outlined" color="primary" onClick={handleUploadDialogClose}>キャンセル</Button>
      </DialogActions>
    </Dialog>
  );

  return uploadDialog;

}

export default UploadDialog;
