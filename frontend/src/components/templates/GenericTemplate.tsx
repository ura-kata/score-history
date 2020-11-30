import React from 'react';
import clsx from "clsx";
import Button from '@material-ui/core/Button';
import { colors, createMuiTheme, createStyles, CssBaseline, makeStyles, Theme, ThemeProvider, Typography, AppBar, Toolbar, IconButton, Drawer, Divider, List, ListItem, ListItemIcon, ListItemText, Container, Box } from '@material-ui/core';
import MenuIcon from "@material-ui/icons/Menu"
import ChevronLeftIcon  from "@material-ui/icons/ChevronLeft"
import CloudUploadIcon from '@material-ui/icons/CloudUpload';
import ViewCarouselIcon from '@material-ui/icons/ViewCarousel';
import ExtensionIcon from '@material-ui/icons/Extension';
import { Link, } from "react-router-dom";
import  Copyright from '../atoms/Copyright'


const drawerWidth = 240;

const theme = createMuiTheme({
  typography: {
    fontFamily: [
      "Noto Sans JP",
      "游ゴシック体",
    ].join(","),
  },
  palette: {
    primary: { main: colors.blueGrey[800] },
  },
});

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root:{
      display: "flex",
    },
    appBar: {
      zIndex: theme.zIndex.drawer + 1,
      transition: theme.transitions.create(["width", "margin"],{
        easing: theme.transitions.easing.sharp,
        duration: theme.transitions.duration.leavingScreen,
      }),
    },
    appBarShift: {
      marginLeft: drawerWidth,
      width: `calc(100% - ${drawerWidth}px)`,
      transition: theme.transitions.create(["width", "margin"], {
        easing: theme.transitions.easing.sharp,
        duration: theme.transitions.duration.enteringScreen,
      })
    },
    toolbar:{
      paddingRight: 24,
    },
    menuButton:{
      marginRight: 36,
    },
    menuButtonHidden: {
      display: "none",
    },
    title:{
      flexGrow: 1,
    },
    drawerPaper: {
      position: "relative",
      whiteSpace: "nowrap",
      width: drawerWidth,
      transition: theme.transitions.create("width", { easing: theme.transitions.easing.sharp, duration: theme.transitions.duration.enteringScreen,})
    },
    drawerPaperClose: {
      overflowX: "hidden",
      transition: theme.transitions.create("width", {easing: theme.transitions.easing.sharp, duration: theme.transitions.duration.leavingScreen,}),
      width: theme.spacing(7),
      [theme.breakpoints.up("sm")]: {
        width: theme.spacing(9),
      },
    },
    toolbarIcon: {
      display: "flex",
      alignItems: "center",
      justifyContent: "flex-end",
      padding: "0 8px",
      ...theme.mixins.toolbar,
    },
    link: {
      textDecoration: "none",
      color: theme.palette.text.secondary,
    },
    content: {
      flexGrow: 1,
      height: "100vh",
      overflow: "auto",
    },
    appBarSpacer: theme.mixins.toolbar,
    container: {
      paddingTop: theme.spacing(4),
      paddingBottom: theme.spacing(4),
    },
    pageTitle: {
      marginBottom: theme.spacing(1),
    },

  })
);

export interface GenericTemplateProps {
  children: React.ReactNode;
  title: string;
}

const GenericTemplate: React.FC<GenericTemplateProps> = ({
  children,
  title,
}) => {
  const classes = useStyles();
  const [open, setOpen] = React.useState(true);

  const handleDrawerOpen = () => {
    setOpen(true);
  };
  const handleDrawerClose = () => {
    setOpen(false);
  }


  return (
    <ThemeProvider theme={theme}>
      <div className={classes.root}>
        <CssBaseline />
        <AppBar position="absolute" className={clsx(classes.appBar, open && classes.appBarShift)}>
          <Toolbar className={classes.toolbar}>
            <IconButton edge="start" color="inherit" aria-label="open drawer" onClick={handleDrawerOpen} className={clsx(classes.menuButton, open && classes.menuButtonHidden)}>
              <MenuIcon />
            </IconButton>
            <Typography component="h1" variant="h6" color="inherit" noWrap className={classes.title}>Practice Manager</Typography>
          </Toolbar>
        </AppBar>
        <Drawer variant="permanent" classes={{paper: clsx(classes.drawerPaper, !open && classes.drawerPaperClose),}} open={open}>
          <div className={classes.toolbarIcon}>
            <IconButton onClick={handleDrawerClose}>
              <ChevronLeftIcon />
            </IconButton>
          </div>
          <Divider />
          <List>
            <Link to="/" className={classes.link}>
              <ListItem button>
                <ListItemIcon>
                  <CloudUploadIcon />
                </ListItemIcon>
                <ListItemText primary="スコアのアップロード" />
              </ListItem>
            </Link>
            <Link to="/display" className={classes.link}>
              <ListItem button>
                <ListItemIcon>
                  <ViewCarouselIcon />
                </ListItemIcon>
                <ListItemText primary="スコアの表示" />
              </ListItem>
            </Link>
            <Link to="/api-test" className={classes.link}>
              <ListItem button>
                <ListItemIcon>
                  <ExtensionIcon />
                </ListItemIcon>
                <ListItemText primary="API Test" />
              </ListItem>
            </Link>

          </List>
        </Drawer>
        <main className={classes.content}>
          <div className={classes.appBarSpacer} />
          <Container maxWidth="lg" className={classes.container}>
            <Typography component="h2" variant="h5" color="inherit" noWrap className={classes.pageTitle}>{title}</Typography>
            {children}
            <Box pt={4}>
              <Copyright />
            </Box>

          </Container>

        </main>

      </div>
    </ThemeProvider>
  );
};

export default GenericTemplate;