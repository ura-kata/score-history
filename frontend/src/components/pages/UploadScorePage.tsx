import React, {useCallback, useMemo} from 'react';
import GenericTemplate from '../templates/GenericTemplate'
import {
  createStyles,
  Button,
  makeStyles,
  Theme,
  colors,
  Grid,
} from '@material-ui/core'
import PracticeManagerApiClient, { SocreVersionMetaUrl} from '../../PracticeManagerApiClient'
import { Alarm } from '@material-ui/icons';
import UploadDialog from '../organisms/UploadDialog';
import VersionDisplayDialog from '../organisms/VersionDisplayDialog'

import ScoreTable, {ScoreTableData} from '../organisms/ScoreTable';
import UpdateDialog from '../organisms/UpdateDialog';

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
      height: "auto",
      width: '100%'
    }
  })
);

const UploadScorePage = () => {
  const classes = useStyles();

  const [selectedScoreName, setSelectedScoreName] = React.useState("");

  const [uploadDialogOpen, setDialogOpen] = React.useState(false);

  const [socreTableDatas, setSocreTableDatas] = React.useState([] as ScoreTableData[])

  const handleUploadDialogOpen = () => {
    setDialogOpen(true);
  };

  const [versionMetaUrlsSet, setVersionMetaUrlsSet] = React.useState<{[scoreName: string]: SocreVersionMetaUrl[]}>({})

  const updateTable = async ()=>{
    const scores = await client.getScores();

      const r = scores.map((x,i)=>({
        open: x.name,
        update:x.name,
        name: x.name,
        title: x.title,
        lastVersion: x.version_meta_urls.slice(-1)[0].version,
        description: x.description
      } as ScoreTableData));
      setSocreTableDatas(r);

      const urlsSet = {} as {[scoreName: string]: SocreVersionMetaUrl[]};
      scores.forEach(x=>{urlsSet[x.name] = x.version_meta_urls})
      setVersionMetaUrlsSet(urlsSet);
  };

  const handleUpdateTable = useCallback(async (event)=>{
    try{
      await updateTable();
    } catch (err){
      alert('更新に失敗しました');
    }
  },[]);

  // ----------------------------------------------------------------------------------------------------------------


  const [updateDialogOpen, setUpdateDialogOpen] = React.useState(false);


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
;


  // ----------------------------------------------------------------------------------------------------------------


  const [versionDialogOpen, setVersionDialogOpen] = React.useState(false);

  const handleVersionDialogClose = useCallback(async ()=>{
    setVersionDialogOpen(false);
  },[]);

  const [displayScoreVersionList, setDisplayScoreVersionList] = React.useState([] as number[]);



  const handleTableSelectedChangeRow = useCallback((scoreName: string)=>{
    setSelectedScoreName(scoreName);
  },[]);

  const handleOpen = useCallback(()=>{

    if(selectedScoreName === ""){
      alert('スコアを選択してください')
      return;
    }
    const urls = versionMetaUrlsSet[selectedScoreName];
    if(!urls) return;
    const versionList = urls.map(x=>x.version);
    setDisplayScoreVersionList(versionList);

    setVersionDialogOpen(true);
  },[selectedScoreName, versionMetaUrlsSet]);

  const handleUpdate = useCallback(()=>{
    if(selectedScoreName === ""){
      alert('スコアを選択してください')
      return;
    }

    setUpdateDialogOpen(true);
  },[selectedScoreName]);

  const scoreTableElement = useMemo(()=>(
    <ScoreTable
      title={""}
      data={socreTableDatas}
      onSelectedChangeRow={handleTableSelectedChangeRow}
    />
  ),[socreTableDatas, handleTableSelectedChangeRow]);

  const handleUpdated = useCallback( async ()=>{
    await updateTable();
    setUpdateDialogOpen(false);
  },[]);
  const handleUpdateCancled = useCallback(()=>{
    setUpdateDialogOpen(false);
  },[]);

  return (
    <GenericTemplate title="スコアの一覧">
      <div>
        <Grid container spacing={3}>
          <Grid item xs={3}>
            <Button variant="outlined" color="primary" onClick={handleUpdateTable}>スコアの取得</Button>
          </Grid>
          <Grid item xs={3}>
            <Button variant="outlined" color="primary" onClick={handleUploadDialogOpen}>アップロードする</Button>
          </Grid>
          <Grid item xs={6} />
          {/*--------------------------------------------------*/}
          <Grid item xs={2}>
            <Button variant="outlined" color="primary" onClick={handleOpen}>表示</Button>
          </Grid>
          <Grid item xs={2}>
            <Button variant="outlined" color="primary" onClick={handleUpdate}>更新</Button>
          </Grid>
          <Grid item xs={8} />
          {/*--------------------------------------------------*/}
          <Grid item xs={12}>
            {/* <div className={classes.table}>
              <DataGrid rows={rows} columns={columns} pageSize={10} />
            </div> */}
            <div className={classes.table}>
              {scoreTableElement}
            </div>
          </Grid>
        </Grid>

        <UploadDialog onUploaded={handlerUploaded} onCanceled={handlerUploadCanceled} open={uploadDialogOpen}/>

        <VersionDisplayDialog
          open={versionDialogOpen}
          scoreName={selectedScoreName}
          versions={displayScoreVersionList}
          onCloseClicked={handleVersionDialogClose}
        />

        <UpdateDialog
          open={updateDialogOpen}
          scoreName={selectedScoreName}
          onUploaded={handleUpdated}
          onCanceled={handleUpdateCancled}

        />

        {/* {updateDialog} */}

      </div>
    </GenericTemplate>
  )
}

export default UploadScorePage;
