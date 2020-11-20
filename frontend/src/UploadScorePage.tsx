import React from 'react';
import GenericTemplate from './GenericTemplate'
import { createStyles, FormControl, FormHelperText, Input, InputLabel, makeStyles, Theme } from '@material-ui/core'

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    img:{
      height: "50vh"
    }

  })
);

const UploadScorePage = () => {
  const classes = useStyles();
  const [file, setFile] = React.useState('');

  const handleUploadClick = (event: any) => {
    console.log();
    let file = event.target.files[0];
    const reader = new FileReader();
    let url = reader.readAsDataURL(file);
    reader.onloadend = (e) =>{
      setFile(reader.result as string);
    }

    console.log(url);
    setFile(file);
  }

  return (
    <GenericTemplate title="アップロードスコア">
      <div>
        <FormControl >
          <InputLabel></InputLabel>
          <Input aria-describedby="" type="file" inputProps={{ accept: "image/*"}} onChange={handleUploadClick}></Input>
          <FormHelperText></FormHelperText>
        </FormControl>
        <img src={file} className={classes.img}></img>
      </div>
    </GenericTemplate>
  )
}

export default UploadScorePage;
