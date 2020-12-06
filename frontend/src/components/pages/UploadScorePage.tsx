import React, {useCallback} from 'react';
import GenericTemplate from '../templates/GenericTemplate'
import { createStyles, FormControl, FormHelperText, Input, Button, InputLabel, makeStyles, Theme, colors, GridList, GridListTile, GridListTileBar, Grid, TextField, Dialog, DialogTitle, DialogContent, DialogActions, Typography, Select, MenuItem } from '@material-ui/core'
import { DialogProps } from '@material-ui/core/Dialog'
import { DataGrid, ColDef, ValueGetterParams, Row, ValueFormatterParams } from '@material-ui/data-grid';
import { useDropzone } from 'react-dropzone'
import { readBuilderProgram } from 'typescript';
import PracticeManagerApiClient, {Score, SocreVersionMetaUrl} from '../../PracticeManagerApiClient'
import { Alarm } from '@material-ui/icons';
import UploadDialog from '../organisms/UploadDialog';

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
  update: {name: string};
  name: string;
  title: string;
  lastVersion: number;
  description: string;
}

const UploadScorePage = () => {
  const classes = useStyles();

  const [uploadDialogOpen, setDialogOpen] = React.useState(false);

  const [rows, setRows] = React.useState([] as Row[])

  const handleUploadDialogOpen = () => {
    setDialogOpen(true);
  };


  const updateTable = async ()=>{
    const scores = await client.getScores();

      const r = scores.map((x,i)=>({
        id: i,
        open: {
          name: x.name,
          versionMetas: x.version_meta_urls
        },
        update:{
          name: x.name
        },
        name: x.name,
        title: x.title,
        lastVersion: x.version_meta_urls.slice(-1)[0].version,
        description: x.description
      }));

      setRows(r);
  };

  const handleUpdateTable = useCallback(async (event)=>{
    try{
      await updateTable();
    } catch (err){
      alert('更新に失敗しました');
    }
  },[]);

  // ----------------------------------------------------------------------------------------------------------------

  const [updateFileDataList, setUpdateFileDataList] = React.useState([] as FileData[]);
  const [updateScoreName, setUpdateScoreName] = React.useState('');

  const [updateDialogOpen, setUpdateDialogOpen] = React.useState(false);
  const [updateDialogScroll, setUpdateDialogScroll] = React.useState<DialogProps['scroll']>('paper');

  const handleUpdateDialogOpen = () => {
    setUpdateDialogOpen(true);
    setUpdateDialogScroll('paper');
  };

  const handleUpdateDialogClose = () => {
    setUpdateDialogOpen(false);
  };


  const handlerUpdate = useCallback(async ()=>{
    try{
      await client.createVersion(updateScoreName, updateFileDataList.map(x=>x.file));
      alert('スコアを更新しました');
      handleUpdateDialogClose();

      setUpdateFileDataList([]);
    } catch(err) {
      alert('スコアの更新に失敗しました');
      return;
    }

    try{
      await updateTable();
    } catch (err){
      alert('更新に失敗しました');
    }
  },[updateFileDataList, updateScoreName]);

  const handlerUploaded = useCallback(async ()=>{
    try{
      await updateTable();
    } catch (err){
      alert('更新に失敗しました');
    }
  }, []);

  const handlerUploadCanceled = useCallback(async ()=>{
    setDialogOpen(false);
  }, []);

  const onUpdateDrop = useCallback((acceptedFiles) => {
    setUpdateFileDataList([])
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
          setUpdateFileDataList([...loadedFileDataList]);

      };

      reader.readAsDataURL(f);
    })

  },[]);
  const updateDrop = useDropzone({onDrop:onUpdateDrop});


  const updateDialog = (
    <Dialog
      open={updateDialogOpen}
      onClose={handleUpdateDialogClose}
      scroll={updateDialogScroll}
    >
      <DialogTitle>新しいバージョン</DialogTitle>
      <DialogContent dividers={true}>
        <Grid container spacing={3}>
          <Grid item xs={12}>
            <Typography>{updateScoreName}</Typography>
          </Grid>
          <Grid item xs={12}>
            <div className={classes.imageDropZoneRoot}>
              <div {...updateDrop.getRootProps()}>
                <input {...updateDrop.getInputProps()} />
                <div className={classes.imageDropZone}>
                  このエリアをクリックするするか画像ドロップしてアップロードしてください
                </div>
              </div>
            </div>
          </Grid>
          <Grid item xs={12}>
            <GridList className={classes.imageList}>
              {
                updateFileDataList.map((fd, i) => (
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
        <Button variant="outlined" color="primary" onClick={handlerUpdate}>更新</Button>
        <Button variant="outlined" color="primary" onClick={handleUpdateDialogClose}>キャンセル</Button>
      </DialogActions>
    </Dialog>
  );

  // ----------------------------------------------------------------------------------------------------------------


  const [versionDialogOpen, setVersionDialogOpen] = React.useState(false);
  const [versionDialogScroll, setVersionDialogScroll] = React.useState<DialogProps['scroll']>('paper');
  const [imageUrls, setImageUrls] = React.useState([] as string[]);

  const handleVersionDialogClose = useCallback(async ()=>{
    setVersionDialogOpen(false);
  },[]);

  const [displayScoreName, setDisplayScoreName] = React.useState('');
  const [displayScoreVersion, setDisplayScoreVersion] = React.useState(-1);
  const [displayScoreVersionList, setDisplayScoreVersionList] = React.useState([] as number[]);


  const changeImageUrls = async (name: string, version: number) => {
    console.log(`changeImageUrls ${name}:${version}`);
    if(name === ''){
      return;
    }
    if(version < 0){
      return;
    }
    console.log(`${name}:${version}`);
    const scoreVersion = await client.getScoreVersion(name, version);
    setImageUrls(scoreVersion.pages.map(x=>x.url));
  };



  const columns: ColDef[] = [
    {
      field: 'open',
      headerName: ' ',
      renderCell: (params: ValueFormatterParams) => {
        const item = (params.value as {name: string, versionMetas: SocreVersionMetaUrl[]});
        console.log(item.name);



        const handleVersionDialogOpenRowButton = ()=>{
          setDisplayScoreName(item.name);
          const versionList = item.versionMetas.map(x=>x.version);
          setDisplayScoreVersionList(versionList);
          const version = versionList.slice(-1)[0];
          setDisplayScoreVersion(version);
          changeImageUrls(item.name, version);
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
    {
      field: 'update',
      headerName: '  ',
      renderCell: (params: ValueFormatterParams) => {
        const name = (params.value as {name: string}).name;
        console.log(name);
        const handleUpdateDialogOpenRowButton = async ()=>{
          setUpdateScoreName(name);

          setUpdateDialogOpen(true);
        };

        return (
          <strong>
            <Button
              variant="contained"
              color="primary"
              size="small"
              style={{margin: 8}}
              onClick={handleUpdateDialogOpenRowButton}
            >更新</Button>
          </strong>
        )
      }
    },
    {field: 'name', headerName: '名前', width: 200},
    {field: 'title', headerName: 'タイトル', width: 200},
    {field: 'lastVersion', headerName: '最新バージョン', width: 200},
    {field: 'description', headerName: '説明', width: 500},
  ];

  const handleDisplayChange = useCallback(async (event)=>{
    const version = event.target.value as number;
    console.log(version);
    setDisplayScoreVersion(version);
    await changeImageUrls(displayScoreName, version);
  }, [displayScoreName]);

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
            <Select
              onChange={handleDisplayChange}
              value={displayScoreVersion}
            >
              {
                displayScoreVersionList.map((x,i)=>(
                <MenuItem value={x} key={i.toString()}>{x.toString()}</MenuItem>
                ))
              }
            </Select>
          </Grid>
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

        <UploadDialog onUploaded={handlerUploaded} onCanceled={handlerUploadCanceled} open={uploadDialogOpen}/>

        {versionDialog}

        {updateDialog}

      </div>
    </GenericTemplate>
  )
}

export default UploadScorePage;
