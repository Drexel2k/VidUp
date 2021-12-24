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
      Result:= true;
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
  if UninstallString <> '' then begin
    UninstallString := RemoveQuotes(UninstallString);
    if Exec(UninstallString, '/SILENT /NORESTART /SUPPRESSMSGBOXES','', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
      Result := 3
    else
      Result := 2;
  end else
    Result := 1;
end;


// add license and privacy dialog /////////////////////////// 
var
  FirstAgreementPage: TOutputMsgMemoWizardPage;
  FirstAgreementPageAcceptedRadio: TRadioButton;
  FirstAgreementPageNotAcceptedRadio: TRadioButton; 
  SecondAgreementPage: TOutputMsgMemoWizardPage;
  SecondAgreementPageAcceptedRadio: TRadioButton;
  SecondAgreementPageNotAcceptedRadio: TRadioButton;   

procedure FirstAgreementPageAccepted(Sender: TObject);
begin
  // update Next button when user (un)accepts the agreement
  WizardForm.NextButton.Enabled := FirstAgreementPageAcceptedRadio.Checked;
end;

procedure SecondAgreementPageAccepted(Sender: TObject);
begin
  // update Next button when user (un)accepts the agreement
  WizardForm.NextButton.Enabled := SecondAgreementPageAcceptedRadio.Checked;
end;

procedure CreateAgreementPages(Sender: TObject);
var
  AgreementFileName: string;
  RichText: AnsiString;
begin
   // create first agreement page
  FirstAgreementPage :=
    CreateOutputMsgMemoPage(
      wpWelcome, SetupMessage(msgWizardLicense), SetupMessage(msgLicenseLabel),
      'Please read the following Licesense Agreement/Terms of Service. You must accept this terms before continuing with the installation. By accepting this terms and using the app you also accept the YouTube Terms of Service.', '');

  FirstAgreementPage.RichEditViewer.Top := 51;
  FirstAgreementPage.RichEditViewer.Height := 177;

  // display file
  AgreementFileName := 'license.rtf';
  ExtractTemporaryFile(AgreementFileName);
  LoadStringFromFile(ExpandConstant('{tmp}\' + AgreementFileName), RichText);
  FirstAgreementPage.RichEditViewer.UseRichEdit := True;
  FirstAgreementPage.RichEditViewer.RTFText := RichText;

  // create buttons
  FirstAgreementPageAcceptedRadio := TRadioButton.Create(WizardForm);
  FirstAgreementPageAcceptedRadio.Parent := FirstAgreementPage.Surface;
  FirstAgreementPageAcceptedRadio.Caption := 'I &accept the agreement';
  FirstAgreementPageAcceptedRadio.Left := 0;
  FirstAgreementPageAcceptedRadio.Top := 330;
  FirstAgreementPageAcceptedRadio.Width := 487;
  FirstAgreementPageAcceptedRadio.Height := 21;
  FirstAgreementPageAcceptedRadio.OnClick := @FirstAgreementPageAccepted;
  FirstAgreementPageNotAcceptedRadio := TRadioButton.Create(WizardForm);
  FirstAgreementPageNotAcceptedRadio.Parent := FirstAgreementPage.Surface;
  FirstAgreementPageNotAcceptedRadio.Caption := 'I &do not accept the agreement';
  FirstAgreementPageNotAcceptedRadio.Left := 0;
  FirstAgreementPageNotAcceptedRadio.Top := 355;
  FirstAgreementPageNotAcceptedRadio.Width := 487;
  FirstAgreementPageNotAcceptedRadio.Height := 21;
  FirstAgreementPageNotAcceptedRadio.OnClick := @FirstAgreementPageAccepted;

  // Initially not accepted
  FirstAgreementPageNotAcceptedRadio.Checked := True;

  // create second agreement page
  SecondAgreementPage :=
    CreateOutputMsgMemoPage(
      100, 'Privacy Policy', SetupMessage(msgLicenseLabel),
      'Please read the following Privacy Policy. You must accept this policy before continuing with the installation. By accepting this privacy policy and using the app you also accept the Google Privacy Policy.', '');

  SecondAgreementPage.RichEditViewer.Top := 51;
  SecondAgreementPage.RichEditViewer.Height := 177;

  // display file
  AgreementFileName := 'privacy.rtf';
  ExtractTemporaryFile(AgreementFileName);
  LoadStringFromFile(ExpandConstant('{tmp}\' + AgreementFileName), RichText);
  SecondAgreementPage.RichEditViewer.UseRichEdit := True;
  SecondAgreementPage.RichEditViewer.RTFText := RichText;

  // create buttons
  SecondAgreementPageAcceptedRadio := TRadioButton.Create(WizardForm);
  SecondAgreementPageAcceptedRadio.Parent := SecondAgreementPage.Surface;
  SecondAgreementPageAcceptedRadio.Caption := 'I &accept the agreement';
  SecondAgreementPageAcceptedRadio.Left := 0;
  SecondAgreementPageAcceptedRadio.Top := 330;
  SecondAgreementPageAcceptedRadio.Width := 487;
  SecondAgreementPageAcceptedRadio.Height := 21;
  SecondAgreementPageAcceptedRadio.OnClick := @SecondAgreementPageAccepted;
  SecondAgreementPageNotAcceptedRadio := TRadioButton.Create(WizardForm);
  SecondAgreementPageNotAcceptedRadio.Parent := SecondAgreementPage.Surface;
  SecondAgreementPageNotAcceptedRadio.Caption := 'I &do not accept the agreement';
  SecondAgreementPageNotAcceptedRadio.Left := 0;
  SecondAgreementPageNotAcceptedRadio.Top := 355;
  SecondAgreementPageNotAcceptedRadio.Width := 487;
  SecondAgreementPageNotAcceptedRadio.Height := 21;
  SecondAgreementPageNotAcceptedRadio.OnClick := @SecondAgreementPageAccepted;

  // Initially not accepted
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
  if CurPageID = FirstAgreementPage.ID then
  begin
    FirstAgreementPageAccepted(nil);
  end;
  if CurPageID = SecondAgreementPage.ID then
  begin
    SecondAgreementPageAccepted(nil);
  end;

end;

procedure InitializeWizard();
begin
  CreateAgreementPages(nil);
end;