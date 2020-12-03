import React, {useCallback} from 'react';
import GenericTemplate from '../templates/GenericTemplate'
import { createStyles, FormControl, FormHelperText, Input, Button, InputLabel, makeStyles, Theme, colors, GridList, GridListTile, GridListTileBar, Grid, TextField, Dialog, DialogTitle, DialogContent, DialogActions } from '@material-ui/core'
import { DialogProps } from '@material-ui/core/Dialog'
import { DataGrid, ColDef, ValueGetterParams, Row, ValueFormatterParams } from '@material-ui/data-grid';
import { useDropzone } from 'react-dropzone'
import { readBuilderProgram } from 'typescript';
import PracticeManagerApiClient, {Score, SocreVersionMetaUrl} from '../../PracticeManagerApiClient'
import { Alarm } from '@material-ui/icons';

import ScoreViewr from '../atoms/ScoreViewer'

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

interface FileData{
  fileUrl: string;
  file: File;
}

interface Row{
  id: number;
  open: {name: string, versionMetas: SocreVersionMetaUrl[]};
  name: string;
  title: string;
  lastVersion: number;
  description: string;
}

const UploadScorePage = () => {
  const classes = useStyles();
  const [fileDataList, setFileDataList] = React.useState([] as FileData[]);
  const [uploadScoreName, setUploadScoreName] = React.useState("");
  const [uploadScoreTitle, setUploadScoreTitle] = React.useState("");
  const [uploadScoreDescription, setUploadScoreDescription] = React.useState("");

  const [uploadDialogOpen, setDialogOpen] = React.useState(false);
  const [uploadDialogScroll, setDialogScroll] = React.useState<DialogProps['scroll']>('paper');

  const [rows, setRows] = React.useState([] as Row[])

  const handleUploadDialogOpen = () => {
    setDialogOpen(true);
    setDialogScroll('paper');
  };

  const handleUploadDialogClose = () => {
    setDialogOpen(false);
  }

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
  const {getRootProps, getInputProps, isDragActive} = useDropzone({onDrop});

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
    }
  },[fileDataList, uploadScoreName, uploadScoreTitle, uploadScoreDescription]);

  const handlerUploadScoreName = useCallback(async (event)=>{
    setUploadScoreName(event.target.value);
  }, []);
  const handlerUploadScoreTitle = useCallback(async (event)=>{
    setUploadScoreTitle(event.target.value);
  }, []);
  const handlerUploadScoreDescription = useCallback(async (event)=>{
    setUploadScoreDescription(event.target.value);
  }, []);

  const handleUpdateTable = useCallback(async (event)=>{
    try{
      const scores = await client.getScores();

      const r = scores.map((x,i)=>({
        id: i,
        open: {
          name: x.name,
          versionMetas: x.version_meta_urls
        },
        name: x.name,
        title: x.title,
        lastVersion: x.version_meta_urls.slice(-1)[0].version,
        description: x.description
      }));

      setRows(r);
    } catch (err){
      alert('更新に失敗しました');
    }
  },[]);


  const uploadDialog = (
    <Dialog
      open={uploadDialogOpen}
      onClose={handleUploadDialogClose}
      scroll={uploadDialogScroll}
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
              <div {...getRootProps()}>
                <input {...getInputProps()} />
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


  const [versionDialogOpen, setVersionDialogOpen] = React.useState(false);
  const [versionDialogScroll, setVersionDialogScroll] = React.useState<DialogProps['scroll']>('paper');
  const [imageUrls, setImageUrls] = React.useState([] as string[]);

  const handleVersionDialogClose = useCallback(async ()=>{
    setVersionDialogOpen(false);
  },[]);

  const columns: ColDef[] = [
    {
      field: 'open',
      headerName: ' ',
      renderCell: (params: ValueFormatterParams) => {
        const item = (params.value as {name: string, versionMetas: SocreVersionMetaUrl[]});
        console.log(item.name);
        const handleVersionDialogOpenRowButton = async ()=>{
          var versionMeta = item.versionMetas.slice(-1)[0];
          const scoreVersion = await client.getScoreVersion(item.name, versionMeta.version);

          setImageUrls(scoreVersion.pages.map(x=>x.url));

          setVersionDialogOpen(true);

        };

        return (
          <strong>
            <Button
              variant="contained"
              color="primary"
              size="small"
              style={{margin: 8}}
              onClick={handleVersionDialogOpenRowButton}
            >Open</Button>
          </strong>
        )
      }
    },
    {field: 'name', headerName: '名前', width: 200},
    {field: 'title', headerName: 'タイトル', width: 200},
    {field: 'lastVersion', headerName: '最新バージョン', width: 200},
    {field: 'description', headerName: '説明', width: 500},
  ];

  const versionDialog = (
    <Dialog
      fullScreen
      open={versionDialogOpen}
      onClose={handleVersionDialogClose}
      scroll={versionDialogScroll}
    >
      <DialogTitle>バージョン表示</DialogTitle>
      <DialogContent dividers={true}>
        <Grid container spacing={3}>
          <Grid item xs={12} style={{textAlign: "center"}}>
            <div style={{width: "90vw", height: "80vh", display: "inline-block"}} >
              <ScoreViewr imageUrls={imageUrls}></ScoreViewr>
            </div>
          </Grid>
        </Grid>

      </DialogContent>
      <DialogActions>
        <Button variant="outlined" color="primary" onClick={handleVersionDialogClose}>閉じる</Button>
      </DialogActions>
    </Dialog>
  );

  return (
    <GenericTemplate title="スコアの一覧">
      <div>
        <Grid container spacing={3}>
          <Grid item xs={12}>
            <Button variant="outlined" color="primary" onClick={handleUpdateTable}>更新</Button>
          </Grid>
          <Grid item xs={12}>
            <div className={classes.table}>
              <DataGrid rows={rows} columns={columns} pageSize={10} />
            </div>
          </Grid>
          <Grid item xs={12}>
            <Button variant="outlined" color="primary" onClick={handleUploadDialogOpen}>アップロードする</Button>
          </Grid>
        </Grid>

        {uploadDialog}

        {versionDialog}

      </div>
    </GenericTemplate>
  )
}

export default UploadScorePage;
