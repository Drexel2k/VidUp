[Setup]
AppName=VidUp
AppId=VidUpDrexelDevelopment
AppVersion=1.12.1
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
DefaultDirName={autopf}\VidUp
DefaultGroupName=VidUp
UninstallDisplayIcon={app}\VidUp.exe
Compression=lzma2
SolidCompression=yes
OutputBaseFilename=VidUp.Setup.Ver.Release.x64
OutputDir=..\VidUp.Setup\bin\Release\

[Files]
Source: ..\VidUp.UI\bin\Release\x64\net5.0-windows\*; DestDir: "{app}"; Flags: recursesubdirs

[Icons]
Name: "{group}\VidUp"; Filename: "{app}\VidUp.exe"

[Code]
// check for old version ////////////////////////
function GetUninstallString(): String;
var
  UninstallPath: String;
  UninstallString: String;
begin
  UninstallPath := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{#emit SetupSetting("AppId")}_is1');
  UninstallString := '';
  RegQueryStringValue(HKLM, UninstallPath, 'UninstallString', UninstallString);
  Result := UninstallString;
end;

function IsNet5Installed(): Boolean;
var
  Net5Path: String;
  Net5String: String;
  Net5FirstChar: String;
begin
  Net5Path := 'Software\dotnet\Setup\InstalledVersions\x64\sharedhost';
  Net5String := '';
  Result := false;
  if RegQueryStringValue(HKLM, Net5Path, 'Version', Net5String) then
  begin
    Net5FirstChar := Copy(Net5String, 1, 1)
    if Net5FirstChar = '5' then
    begin
      Result:= true;
    end;
  end
end;


function IsUpgrade(): Boolean;
begin
  Result := (GetUninstallString() <> '');
end;


function UnInstallOldVersion(): Integer;
var
  UninstallString: String;
  ResultCode: Integer;
begin
{ Return Values: }
{ 1 - uninstall string is empty }
{ 2 - error executing the UnInstallString }
{ 3 - successfully executed the UnInstallString }

  { default return value }
  Result := 0;

  { get the uninstall string of the old app }
  UninstallString := GetUninstallString();
  if UninstallString <> '' then
  begin
    UninstallString := RemoveQuotes(UninstallString);
    if Exec(UninstallString, '/SILENT /NORESTART /SUPPRESSMSGBOXES','', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    begin
      Result := 3
    end else
    begin
      Result := 2;
    end;
  end else
  begin
    Result := 1;
  end;
end;


// add license and privacy dialog /////////////////////////// 
var
  FirstAgreementPage: TOutputMsgMemoWizardPage;
  FirstAgreementPageAcceptedRadio: TRadioButton;
  FirstAgreementPageNotAcceptedRadio: TRadioButton; 
  SecondAgreementPage: TOutputMsgMemoWizardPage;
  SecondAgreementPageAcceptedRadio: TRadioButton;
  SecondAgreementPageNotAcceptedRadio: TRadioButton;   

procedure AgreementPageAccepted(Sender: TObject);
begin
  if (Sender = FirstAgreementPageAcceptedRadio) or (Sender = FirstAgreementPageNotAcceptedRadio) then
  begin
    // update Next button when user (un)accepts the agreement
    WizardForm.NextButton.Enabled := FirstAgreementPageAcceptedRadio.Checked;
  end;

  if (Sender = SecondAgreementPageAcceptedRadio) or (Sender = SecondAgreementPageNotAcceptedRadio) then
  begin
    // update Next button when user (un)accepts the agreement
    WizardForm.NextButton.Enabled := SecondAgreementPageAcceptedRadio.Checked;
  end;
end;

procedure SecondAgreementPageAccepted(Sender: TObject);
begin
  // update Next button when user (un)accepts the agreement
  WizardForm.NextButton.Enabled := SecondAgreementPageAcceptedRadio.Checked;
end;

function CreateRadioButton(Parent: TNewNotebookPage; Top: Integer; Caption: String): TRadioButton;
begin
  Result := TRadioButton.Create(WizardForm);
  Result.Parent := Parent;
  Result.Caption := Caption;
  Result.Left := 0;
  Result.Top := Top;
  Result.Width := 487;
  Result.Height := 21;
  Result.Anchors := [akLeft, akBottom];
  Result.OnClick := @AgreementPageAccepted;
end;

function CreateAgreementPage(AfterID: Integer; Caption: String; Description: String; RtfFile: String): TOutputMsgMemoWizardPage;
var
  RichText: AnsiString;
begin
   // create first agreement page
  Result :=
    CreateOutputMsgMemoPage(
      AfterID, Caption, SetupMessage(msgLicenseLabel),
      Description, '');

  Result.RichEditViewer.Top := 51;
  Result.RichEditViewer.Height := 177;

  // display file
  ExtractTemporaryFile(RtfFile);
  LoadStringFromFile(ExpandConstant('{tmp}\' + RtfFile), RichText);
  Result.RichEditViewer.UseRichEdit := True;
  Result.RichEditViewer.RTFText := RichText;
end;

procedure CreateAgreementPages();
begin
   // create first agreement page
  FirstAgreementPage := CreateAgreementPage(wpWelcome, SetupMessage(msgWizardLicense),
    'Please read the following Licesense Agreement/Terms of Service. You must accept this terms before continuing with the installation. By accepting this terms and using the app you also accept the YouTube Terms of Service.',
    'license.rtf');

  // create buttons
  FirstAgreementPageAcceptedRadio := CreateRadioButton(FirstAgreementPage.Surface, 241, 'I &accept the agreement');
  FirstAgreementPageNotAcceptedRadio := CreateRadioButton(FirstAgreementPage.Surface, 266, 'I &do not accept the agreement');

  // initially not accepted
  FirstAgreementPageNotAcceptedRadio.Checked := True;

  // create second agreement page
  SecondAgreementPage := CreateAgreementPage(100, 'Privacy Policy',
    'Please read the following Privacy Policy. You must accept this policy before continuing with the installation. By accepting this privacy policy and using the app you also accept the Google Privacy Policy.',
    'privacy.rtf');

  // create buttons
  SecondAgreementPageAcceptedRadio := CreateRadioButton(SecondAgreementPage.Surface, 241, 'I &accept the agreement');
  SecondAgreementPageNotAcceptedRadio := CreateRadioButton(SecondAgreementPage.Surface, 266, 'I &do not accept the agreement');

  // initially not accepted
  SecondAgreementPageNotAcceptedRadio.Checked := True;
end;

// event registrations //////////////////////////
function InitializeSetup(): Boolean;
begin
    Result := true;
    if not IsNet5Installed() then
    begin
        MsgBox('.NET 5 ist required to run this app, please download from https://dotnet.microsoft.com/download/dotnet/5.0 and install it.', mbInformation, MB_OK);
        Result := false;
    end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if (CurStep=ssInstall) then
  begin
    if (IsUpgrade()) then
    begin
      ResultCode := UnInstallOldVersion();
      if(ResultCode <> 3) then
      begin
        MsgBox('Could not unstiall old version. Please remove it manually.', mbInformation, MB_OK);
        Abort();
      end;
    end; 
  end;
end;


procedure CurPageChanged(CurPageID: Integer);
begin
  // Update Next button when user gets to agreement pages
  if (CurPageID = FirstAgreementPage.ID) or (CurPageID = SecondAgreementPage.ID) then
  begin
    WizardForm.NextButton.Enabled := false;
  end;
end;

procedure InitializeWizard();
begin
  CreateAgreementPages();
end;