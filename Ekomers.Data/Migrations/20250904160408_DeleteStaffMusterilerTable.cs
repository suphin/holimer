using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class DeleteStaffMusterilerTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ACCEPTEDESP",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "ACCEPTEINV",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "ACCEPTEINVPUBLIC",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "ACTIVE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "ADDR1",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "ADDR2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "ADDTOREFLIST",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "ADRESSNO",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "APPLEID",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "ARPQUOTEINC",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "AUTOPAIDBANK",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKACCOUNTS1",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKACCOUNTS2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKACCOUNTS3",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKACCOUNTS4",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKACCOUNTS5",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKACCOUNTS6",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKACCOUNTS7",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKBCURRENCY1",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKBCURRENCY2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKBCURRENCY3",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKBCURRENCY4",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKBCURRENCY5",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKBCURRENCY6",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKBCURRENCY7",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKBICS1",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKBICS2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKBICS3",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKBICS4",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKBICS5",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKBICS6",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKBICS7",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKBRANCHS1",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKBRANCHS2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKBRANCHS3",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKBRANCHS4",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKBRANCHS5",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKBRANCHS6",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKBRANCHS7",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKCORRPACC1",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKCORRPACC2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKCORRPACC3",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKCORRPACC4",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKCORRPACC5",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKCORRPACC6",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKCORRPACC7",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKIBANS1",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKIBANS2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKIBANS3",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKIBANS4",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKIBANS5",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKIBANS6",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKIBANS7",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKNAMES1",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKNAMES2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKNAMES3",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKNAMES4",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKNAMES5",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKNAMES6",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKNAMES7",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKVOEN1",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKVOEN2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKVOEN3",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKVOEN4",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKVOEN5",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKVOEN6",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BANKVOEN7",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BLOCKED",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "BROKERCOMP",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CAPIBLOCK_CREADEDDATE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CAPIBLOCK_CREATEDBY",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CAPIBLOCK_CREATEDHOUR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CAPIBLOCK_CREATEDMIN",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CAPIBLOCK_CREATEDSEC",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CAPIBLOCK_MODIFIEDBY",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CAPIBLOCK_MODIFIEDDATE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CAPIBLOCK_MODIFIEDHOUR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CAPIBLOCK_MODIFIEDMIN",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CAPIBLOCK_MODIFIEDSEC",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CARDTYPE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CASHREF",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CCURRENCY",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CELLPHONE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CITY",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CITYCODE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CITYID",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CLANGUAGE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CLCCANDEDUCT",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CLCRM",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CLORDFREQ",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CLOSEDATECOUNT",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CLOSEDATETRACK",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CLPTYPEFORPPAYDT",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CLSTYPEFORPPAYDT",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CODE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "COLLECTINVOICING",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "COMMRECORDNO",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CONSCODEREF",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "COUNTRY",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "COUNTRYCODE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CRATEDIFFPROC",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CREATEWHFICHE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CURRATETYPE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "CYPHCODE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSBANKCURRENCY1",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSBANKCURRENCY2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSBANKCURRENCY3",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSBANKCURRENCY4",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSBANKCURRENCY5",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSBANKCURRENCY6",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSBANKCURRENCY7",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSBANKNO1",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSBANKNO2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSBANKNO3",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSBANKNO4",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSBANKNO5",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSBANKNO6",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSBANKNO7",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSLIMIT1",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSLIMIT2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSLIMIT3",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSLIMIT4",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSLIMIT5",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSLIMIT6",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSLIMIT7",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSRISKCNTRL1",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSRISKCNTRL2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSRISKCNTRL3",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSRISKCNTRL4",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSRISKCNTRL5",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSRISKCNTRL6",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSRISKCNTRL7",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSTOTAL1",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSTOTAL2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSTOTAL3",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSTOTAL4",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSTOTAL5",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSTOTAL6",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DBSTOTAL7",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DEFBNACCREF",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DEFINITION2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DEFINITION_",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DEGACTIVE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DEGCURR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DELIVERYFIRM",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DELIVERYMETHOD",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DISCRATE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DISCTYPE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DISPPRINTCNT",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DISTRICT",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DISTRICTCODE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DRIVERREF",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DSPSENDEMAILADDR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DSPSENDFAXNR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DSPSENDFORMAT",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DSPSENDMETHOD",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DUEDATECONTROL1",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DUEDATECONTROL10",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DUEDATECONTROL11",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DUEDATECONTROL12",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DUEDATECONTROL13",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DUEDATECONTROL14",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DUEDATECONTROL15",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DUEDATECONTROL2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DUEDATECONTROL3",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DUEDATECONTROL4",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DUEDATECONTROL5",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DUEDATECONTROL6",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DUEDATECONTROL7",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DUEDATECONTROL8",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DUEDATECONTROL9",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DUEDATECOUNT",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DUEDATELIMIT",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "DUEDATETRACK",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EARCEMAILADDR1",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EARCEMAILADDR2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EARCEMAILADDR3",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EBANKNO",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EBUSDATASENDTYPE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EDINO",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EINVCUSTOM",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EINVOICEID",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EINVOICETYP",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EINVOICETYPE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EMAILADDR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EMAILADDR2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EMAILADDR3",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EXCNTRYREF",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EXCNTRYTYP",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EXIMBRBANKREF",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EXIMCNSLTCLREF",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EXIMCUSTOMREF",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EXIMFRGHTCLREF",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EXIMNTFYCLREF",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EXIMPAYTYPREF",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EXIMREGTYPREF",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EXIMSENDEMAILADDR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EXIMSENDFAXNR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EXIMSENDFORMAT",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EXIMSENDMETHOD",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EXPBRWS",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EXPBUSTYPREF",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EXPDOCNO",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EXPREGNO",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EXTACCESSFLAGS",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EXTENREF",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EXTSENDEMAILADDR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EXTSENDFAXNR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EXTSENDFORMAT",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "EXTSENDMETHOD",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "FACEBOOKURL",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "FACTORYDIVNR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "FACTORYNR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "FAXCODE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "FAXEXTNUM",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "FAXNR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "FBASENDEMAILADDR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "FBASENDFAXNR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "FBASENDFORMAT",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "FBASENDMETHOD",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "FBSSENDEMAILADDR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "FBSSENDFAXNR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "FBSSENDFORMAT",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "FBSSENDMETHOD",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "FINBRWS",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "GLOBALID",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "GRPFIRMNR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "GUID",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "IMAGEINC",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "IMCNTRYREF",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "IMCNTRYTYP",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "IMPBRWS",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "INCHARGE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "INCHARGE2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "INCHARGE3",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "INCHTELCODES1",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "INCHTELCODES2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "INCHTELCODES3",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "INCHTELEXTNUMS1",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "INCHTELEXTNUMS2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "INCHTELEXTNUMS3",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "INCHTELNRS1",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "INCHTELNRS2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "INCHTELNRS3",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "ININVENNR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "INISTATUSFLAGS",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "INSTAGRAMURL",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "INSTEADOFDESP",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "INVPRINTCNT",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "INVSENDEMAILADDR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "INVSENDFAXNR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "INVSENDFORMAT",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "INVSENDMETHOD",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "ISFOREIGN",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "ISPERCURR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "ISPERSCOMP",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "KVKKANONYDATE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "KVKKANONYSTATUS",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "KVKKBEGDATE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "KVKKCANCELDATE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "KVKKENDDATE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "KVKKPERMSTATUS",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "LABELINFO",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "LABELINFODESP",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "LASTSENDREMLEV",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "LATITUTE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "LDXFIRMNR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "LIDCONFIRMED",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "LINKEDINURL",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "LOANGRPCTRL",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "LOGOID",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "LONGITUDE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "LOWLEVELCODES1",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "LOWLEVELCODES10",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "LOWLEVELCODES2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "LOWLEVELCODES3",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "LOWLEVELCODES4",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "LOWLEVELCODES5",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "LOWLEVELCODES6",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "LOWLEVELCODES7",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "LOWLEVELCODES8",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "LOWLEVELCODES9",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "LTRSENDEMAILADDR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "LTRSENDFAXNR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "LTRSENDFORMAT",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "LTRSENDMETHOD",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "MAPID",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "MERSISNO",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "NAME",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "NOTIFYCRDREF",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "OFFSENDEMAILADDR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "OFFSENDFAXNR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "OFFSENDFORMAT",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "OFFSENDMETHOD",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "ORDDAY",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "ORDPRINTCNT",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "ORDPRIORITY",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "ORDSENDEMAILADDR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "ORDSENDFAXNR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "ORDSENDFORMAT",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "ORDSENDMETHOD",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "ORGLOGICREF",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "ORGLOGOID",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "OUTINVENNR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "OVERLAPAMNT",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "OVERLAPPERC",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "OVERLAPTYPE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "PARENTCLREF",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "PAYMENTPROC",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "PAYMENTPROCBRANCH",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "PAYMENTREF",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "PAYMENTTYPE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "PERSONELCOSTS",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "PIECEORDINFLICT",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "POSTCODE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "POSTLABELCODE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "POSTLABELCODEDESP",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "PPGROUPCODE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "PPGROUPREF",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "PROFILEID",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "PROFILEIDDESP",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "PROJECTREF",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "PUBLICBNACCREF",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "PURCHBRWS",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "PURCORDERPRICE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "PURCORDERSTATUS",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "QTYDEPDURATION",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "QTYINDEPDURATION",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "RECSTATUS",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "REMSENDFORMAT",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "RSKAGINGCR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "RSKAGINGDAY",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "RSKDUEDATECR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "RSKLIMCR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "SALESBRWS",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "SAMEITEMCODEUSE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "SECTORMAINREF",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "SECTORSUBREF",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "SENDERLABELCODE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "SENDERLABELCODEDESP",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "SENDMOD",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "SHIPBEGTIME1",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "SHIPBEGTIME2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "SHIPBEGTIME3",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "SHIPENDTIME1",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "SHIPENDTIME2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "SHIPENDTIME3",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "SITEID",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "SKYPEID",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "SLSORDERPRICE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "SLSORDERSTATUS",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "SPECODE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "SPECODE2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "SPECODE3",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "SPECODE4",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "SPECODE5",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "STATECODE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "STATENAME",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "STORECREDITCARDNO",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "SUBCONT",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "SUBSCRIBEREXT",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "SUBSCRIBERSTAT",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "SURNAME",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "TAXNR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "TAXOFFCODE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "TAXOFFICE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "TCKNO",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "TELCODES1",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "TELCODES2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "TELEXTNUMS1",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "TELEXTNUMS2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "TELNRS1",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "TELNRS2",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "TEXTINC",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "TEXTREFEN",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "TEXTREFTR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "TOWN",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "TOWNCODE",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "TOWNID",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "TRADINGGRP",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "TWITTERURL",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "USEDINPERIODS",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "VATNR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "WARNEMAILADDR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "WARNFAXNR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "WARNMETHOD",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "WEBADDR",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "WFLOWCRDREF",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "WFSTATUS",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "WHATSAPPID",
                table: "Musteriler");

            migrationBuilder.AlterColumn<string>(
                name: "LOGICALREF",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "LOGICALREF",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ACCEPTEDESP",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ACCEPTEINV",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ACCEPTEINVPUBLIC",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ACTIVE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ADDR1",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ADDR2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ADDTOREFLIST",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ADRESSNO",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "APPLEID",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ARPQUOTEINC",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AUTOPAIDBANK",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKACCOUNTS1",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKACCOUNTS2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKACCOUNTS3",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKACCOUNTS4",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKACCOUNTS5",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKACCOUNTS6",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKACCOUNTS7",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKBCURRENCY1",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKBCURRENCY2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKBCURRENCY3",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKBCURRENCY4",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKBCURRENCY5",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKBCURRENCY6",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKBCURRENCY7",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKBICS1",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKBICS2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKBICS3",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKBICS4",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKBICS5",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKBICS6",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKBICS7",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKBRANCHS1",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKBRANCHS2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKBRANCHS3",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKBRANCHS4",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKBRANCHS5",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKBRANCHS6",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKBRANCHS7",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKCORRPACC1",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKCORRPACC2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKCORRPACC3",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKCORRPACC4",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKCORRPACC5",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKCORRPACC6",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKCORRPACC7",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKIBANS1",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKIBANS2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKIBANS3",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKIBANS4",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKIBANS5",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKIBANS6",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKIBANS7",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKNAMES1",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKNAMES2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKNAMES3",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKNAMES4",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKNAMES5",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKNAMES6",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKNAMES7",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKVOEN1",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKVOEN2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKVOEN3",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKVOEN4",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKVOEN5",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKVOEN6",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BANKVOEN7",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BLOCKED",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BROKERCOMP",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CAPIBLOCK_CREADEDDATE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CAPIBLOCK_CREATEDBY",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CAPIBLOCK_CREATEDHOUR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CAPIBLOCK_CREATEDMIN",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CAPIBLOCK_CREATEDSEC",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CAPIBLOCK_MODIFIEDBY",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CAPIBLOCK_MODIFIEDDATE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CAPIBLOCK_MODIFIEDHOUR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CAPIBLOCK_MODIFIEDMIN",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CAPIBLOCK_MODIFIEDSEC",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CARDTYPE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CASHREF",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CCURRENCY",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CELLPHONE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CITY",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CITYCODE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CITYID",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CLANGUAGE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CLCCANDEDUCT",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CLCRM",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CLORDFREQ",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CLOSEDATECOUNT",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CLOSEDATETRACK",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CLPTYPEFORPPAYDT",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CLSTYPEFORPPAYDT",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CODE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "COLLECTINVOICING",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "COMMRECORDNO",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CONSCODEREF",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "COUNTRY",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "COUNTRYCODE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CRATEDIFFPROC",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CREATEWHFICHE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CURRATETYPE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CYPHCODE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSBANKCURRENCY1",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSBANKCURRENCY2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSBANKCURRENCY3",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSBANKCURRENCY4",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSBANKCURRENCY5",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSBANKCURRENCY6",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSBANKCURRENCY7",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSBANKNO1",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSBANKNO2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSBANKNO3",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSBANKNO4",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSBANKNO5",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSBANKNO6",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSBANKNO7",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSLIMIT1",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSLIMIT2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSLIMIT3",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSLIMIT4",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSLIMIT5",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSLIMIT6",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSLIMIT7",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSRISKCNTRL1",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSRISKCNTRL2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSRISKCNTRL3",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSRISKCNTRL4",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSRISKCNTRL5",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSRISKCNTRL6",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSRISKCNTRL7",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSTOTAL1",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSTOTAL2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSTOTAL3",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSTOTAL4",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSTOTAL5",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSTOTAL6",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DBSTOTAL7",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DEFBNACCREF",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DEFINITION2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DEFINITION_",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DEGACTIVE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DEGCURR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DELIVERYFIRM",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DELIVERYMETHOD",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DISCRATE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DISCTYPE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DISPPRINTCNT",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DISTRICT",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DISTRICTCODE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DRIVERREF",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DSPSENDEMAILADDR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DSPSENDFAXNR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DSPSENDFORMAT",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DSPSENDMETHOD",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DUEDATECONTROL1",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DUEDATECONTROL10",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DUEDATECONTROL11",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DUEDATECONTROL12",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DUEDATECONTROL13",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DUEDATECONTROL14",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DUEDATECONTROL15",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DUEDATECONTROL2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DUEDATECONTROL3",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DUEDATECONTROL4",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DUEDATECONTROL5",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DUEDATECONTROL6",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DUEDATECONTROL7",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DUEDATECONTROL8",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DUEDATECONTROL9",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DUEDATECOUNT",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DUEDATELIMIT",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DUEDATETRACK",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EARCEMAILADDR1",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EARCEMAILADDR2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EARCEMAILADDR3",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EBANKNO",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EBUSDATASENDTYPE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EDINO",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EINVCUSTOM",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EINVOICEID",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EINVOICETYP",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EINVOICETYPE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EMAILADDR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EMAILADDR2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EMAILADDR3",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EXCNTRYREF",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EXCNTRYTYP",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EXIMBRBANKREF",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EXIMCNSLTCLREF",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EXIMCUSTOMREF",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EXIMFRGHTCLREF",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EXIMNTFYCLREF",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EXIMPAYTYPREF",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EXIMREGTYPREF",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EXIMSENDEMAILADDR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EXIMSENDFAXNR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EXIMSENDFORMAT",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EXIMSENDMETHOD",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EXPBRWS",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EXPBUSTYPREF",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EXPDOCNO",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EXPREGNO",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EXTACCESSFLAGS",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EXTENREF",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EXTSENDEMAILADDR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EXTSENDFAXNR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EXTSENDFORMAT",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EXTSENDMETHOD",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FACEBOOKURL",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FACTORYDIVNR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FACTORYNR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FAXCODE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FAXEXTNUM",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FAXNR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FBASENDEMAILADDR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FBASENDFAXNR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FBASENDFORMAT",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FBASENDMETHOD",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FBSSENDEMAILADDR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FBSSENDFAXNR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FBSSENDFORMAT",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FBSSENDMETHOD",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FINBRWS",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GLOBALID",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GRPFIRMNR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GUID",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IMAGEINC",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IMCNTRYREF",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IMCNTRYTYP",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IMPBRWS",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "INCHARGE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "INCHARGE2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "INCHARGE3",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "INCHTELCODES1",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "INCHTELCODES2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "INCHTELCODES3",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "INCHTELEXTNUMS1",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "INCHTELEXTNUMS2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "INCHTELEXTNUMS3",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "INCHTELNRS1",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "INCHTELNRS2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "INCHTELNRS3",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ININVENNR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "INISTATUSFLAGS",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "INSTAGRAMURL",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "INSTEADOFDESP",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "INVPRINTCNT",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "INVSENDEMAILADDR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "INVSENDFAXNR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "INVSENDFORMAT",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "INVSENDMETHOD",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ISFOREIGN",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ISPERCURR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ISPERSCOMP",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "KVKKANONYDATE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "KVKKANONYSTATUS",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "KVKKBEGDATE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "KVKKCANCELDATE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "KVKKENDDATE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "KVKKPERMSTATUS",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LABELINFO",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LABELINFODESP",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LASTSENDREMLEV",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LATITUTE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LDXFIRMNR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LIDCONFIRMED",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LINKEDINURL",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LOANGRPCTRL",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LOGOID",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LONGITUDE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LOWLEVELCODES1",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LOWLEVELCODES10",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LOWLEVELCODES2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LOWLEVELCODES3",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LOWLEVELCODES4",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LOWLEVELCODES5",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LOWLEVELCODES6",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LOWLEVELCODES7",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LOWLEVELCODES8",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LOWLEVELCODES9",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LTRSENDEMAILADDR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LTRSENDFAXNR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LTRSENDFORMAT",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LTRSENDMETHOD",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MAPID",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MERSISNO",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NAME",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NOTIFYCRDREF",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OFFSENDEMAILADDR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OFFSENDFAXNR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OFFSENDFORMAT",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OFFSENDMETHOD",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ORDDAY",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ORDPRINTCNT",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ORDPRIORITY",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ORDSENDEMAILADDR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ORDSENDFAXNR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ORDSENDFORMAT",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ORDSENDMETHOD",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ORGLOGICREF",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ORGLOGOID",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OUTINVENNR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OVERLAPAMNT",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OVERLAPPERC",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OVERLAPTYPE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PARENTCLREF",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PAYMENTPROC",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PAYMENTPROCBRANCH",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PAYMENTREF",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PAYMENTTYPE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PERSONELCOSTS",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PIECEORDINFLICT",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "POSTCODE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "POSTLABELCODE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "POSTLABELCODEDESP",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PPGROUPCODE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PPGROUPREF",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PROFILEID",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PROFILEIDDESP",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PROJECTREF",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PUBLICBNACCREF",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PURCHBRWS",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PURCORDERPRICE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PURCORDERSTATUS",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "QTYDEPDURATION",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "QTYINDEPDURATION",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RECSTATUS",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "REMSENDFORMAT",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RSKAGINGCR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RSKAGINGDAY",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RSKDUEDATECR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RSKLIMCR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SALESBRWS",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SAMEITEMCODEUSE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SECTORMAINREF",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SECTORSUBREF",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SENDERLABELCODE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SENDERLABELCODEDESP",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SENDMOD",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SHIPBEGTIME1",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SHIPBEGTIME2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SHIPBEGTIME3",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SHIPENDTIME1",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SHIPENDTIME2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SHIPENDTIME3",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SITEID",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SKYPEID",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SLSORDERPRICE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SLSORDERSTATUS",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SPECODE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SPECODE2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SPECODE3",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SPECODE4",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SPECODE5",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "STATECODE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "STATENAME",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "STORECREDITCARDNO",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SUBCONT",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SUBSCRIBEREXT",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SUBSCRIBERSTAT",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SURNAME",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TAXNR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TAXOFFCODE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TAXOFFICE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TCKNO",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TELCODES1",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TELCODES2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TELEXTNUMS1",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TELEXTNUMS2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TELNRS1",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TELNRS2",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TEXTINC",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TEXTREFEN",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TEXTREFTR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TOWN",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TOWNCODE",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TOWNID",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TRADINGGRP",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TWITTERURL",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "USEDINPERIODS",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VATNR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WARNEMAILADDR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WARNFAXNR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WARNMETHOD",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WEBADDR",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WFLOWCRDREF",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WFSTATUS",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WHATSAPPID",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
