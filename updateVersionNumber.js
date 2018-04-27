/*
Node tool to automate updating configuration files during the release process
args: [gameFolderName] [update type (major|minor|patch)]
example: node updateVersionNumber.js MagicChance minor
*/
//instructions
if ( process.argv.length < 3 )
{
    process.stdout.write( "-- HELP - Use syntax: \n'node updateVersionNumber.js [gameFolderName] [updateType (major|minor|patch)]'\n" );
    return;
}
//libraries
let fs = require( 'fs' );
var semver = require( "semver" );

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

process.stdout.write( "Retrieving old version number: " );
let oldVersionNumber = getOldVersionFromConfig( configFilePath );
process.stdout.write( oldVersionNumber + "\n" );
process.stdout.write( "Calculating new version number: " );
let newVersionNumber = semver.inc( oldVersionNumber, updateType );
if ( newVersionNumber == null )
{
    throw new Error( "Improper update type in args: " + updateType + ".  Please use 'major', 'minor', or 'patch'." )
}
process.stdout.write( newVersionNumber + "\n" );

//update the config json
process.stdout.write( "--Updating version number in " + configFileName + "\n" );
createNewConfigJson( newVersionNumber, configFilePath );
//update the asset config json
process.stdout.write( "--Updating version number in " + remoteAssetConfigFileName + "\n" );
createNewRemoteAssetsConfigJson( newVersionNumber, remoteAssetsConfigFilePath );
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

function getOldVersionFromConfig ( configFilePath )
{
    //get buffer from the config file
    try
    {
        var versionConfigFile = fs.readFileSync( configFilePath );
    }
    catch ( err )
    {
        if ( err.code === 'ENOENT' )
        {
            throw new Error( "Unable to read file " + configFilePath + ", please ensure the path is correct." );
        }
        else
        {
            console.log( err );
        }
        return;
    }
    //parse the json for version data
    try
    {
        var versionConfigJson = JSON.parse( versionConfigFile );
    }
    catch ( err )
    {
        throw new Error( "Unable to parse json file: " + configFilePath + ", please ensure the path is correct and the json is uncorrupted" );
        return;
    }
    //get the old version number
    try
    {
        var version = versionConfigJson[ "games" ][ 0 ].version;
    }
    catch ( err )
    {
        throw new Error( "Unable to find version number in json at ['games'][0].version.  Please ensure json is correctly formatted in " + configFilePath );
        return;
    }
    if ( semver.valid( version ) == null )
    {
        throw new Error( "Version improperly formatted in " + configFilePath + "\n" );
        return;
    }
    return version;
}

function writeJsonToFile ( path, jsonContent )
{
    fs.writeFile( path, JSON.stringify( jsonContent, null, 4 ), function ( err )
    {
        if ( err ) return console.log( err );
    } );
}
