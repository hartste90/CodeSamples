/*
Node tool to automate updating configuration files during the release process
args: [gameFolderName] [update type (minor|major)]
example: node updateVersionNumber.js minor
*/
//libraries
let fs = require( 'fs' );

//args
let gameFolderName = process.argv[ 2 ];
let updateType = process.argv[ 3 ];

//const
const gamePathPredicate = "../UnityGames/";
const gamePathAnticate = "/Config/";
const configFileName = "config.json";
const packageFileName = "package.json";
const remoteAssetConfigFileName = "remote_assets_config.json";

/*--Node Main--*/
//setup the file paths to the correct game folder
process.stdout.write( "Setting up paths for " + gameFolderName + "\n" );
let configFolderPathHeader = gamePathPredicate + gameFolderName + gamePathAnticate;
let configFilePath = configFolderPathHeader + configFileName;
let packageFilePath = configFolderPathHeader + packageFileName;
let remoteAssetsConfigFilePath = configFolderPathHeader + remoteAssetConfigFileName;

let oldVersionNumber = getOldVersionFromPackage( packageFilePath );
let newVersionNumber = calculateNewVersionNumber( oldVersionNumber, updateType );

//update the config json
process.stdout.write( "--Updating version number in " + configFileName + "\n" );
createNewConfigJson( newVersionNumber, configFilePath );
//update the asset config json
process.stdout.write( "--Updating version number in " + remoteAssetConfigFileName + "\n" );
//update the package json
process.stdout.write( "--Updating version number in " + packageFileName + "\n" );
createNewPackageJson( newVersionNumber, packageFilePath );

function createNewPackageJson ( newVersion, packageFilePath )
{
    let packageFile = fs.readFileSync( packageFilePath );
    let packageJson = JSON.parse( packageFile );
    packageJson.version = newVersion;
    writeJsonToFile( packageFilePath, packageJson );
}

function createNewRemoteAssetsConfigJson ( newVersion, remoteAssetConfigFilePath )
{
    let remoteAssetConfigFile = fs.readFileSync( remoteAssetsConfigFilePath );
    let remoteAssetConfigJson = JSON.parse( remoteAssetConfigFile );
    remoteAssetConfigJson.gameVersion = newVersion;
    writeJsonToFile( remoteAssetConfigFilePath, remoteAssetConfigJson );
}

function createNewConfigJson ( newVersion, configFilePath )
{
    let configFile = fs.readFileSync( configFilePath );
    let configJson = JSON.parse( configFile );
    configJson[ "games" ][ 0 ].version = newVersion;
    writeJsonToFile( configFilePath, configJson );
}

function getOldVersionFromPackage ( versionPackageFilePath )
{
    process.stdout.write( "Retrieving old version number: " );

    //read in the json file
    let versionPackageFile = fs.readFileSync( versionPackageFilePath );
    let versionPackageJson = JSON.parse( versionPackageFile );
    //get the old version number
    let version = versionPackageJson.version;
    if ( typeof version == undefined )
    {
        process.stdout.write( "ERROR: version not found in " + versionPackageFilePath + "\n" );
        return;
    }
    process.stdout.write( version + "\n" );
    return version;
}

function calculateNewVersionNumber ( currentVersionNumber, updateType )
{

    process.stdout.write( "Calculating new version number: " );
    //split the version number into it's 3 parts
    let versionAsArray = currentVersionNumber.split( '.' );
    //update the correct part of the version number based on the update type
    if ( updateType == "major" )
    {
        versionAsArray[ 1 ] = parseInt( versionAsArray[ 1 ] ) + 1;

        versionAsArray[ 2 ] = 0;
    }
    else if ( updateType == "minor" )
    {
        versionAsArray[ 2 ] = parseInt( versionAsArray[ 2 ] ) + 1;
    }
    else
    {
        return Error( 'Please specify an update type from ("major" or "minor")' );
    }
    //format the new version number
    let newVersion = versionAsArray.join( '.' );
    process.stdout.write( newVersion + "\n" );
    return newVersion;
}

function writeJsonToFile ( path, jsonContent )
{
    fs.writeFile( path, JSON.stringify( jsonContent ), function ( err )
    {
        if ( err ) return console.log( err );
    } );
}
