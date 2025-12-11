namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Specifies the interface type for a network adapter, as defined by the Windows API and IANA standards. This
    /// enumeration provides values that identify the physical or logical technology used by a network interface, such
    /// as Ethernet, PPP, loopback, wireless, and many others.
    /// </summary>
    /// <remarks>The values in this enumeration correspond to standardized interface types defined by the
    /// Internet Assigned Numbers Authority (IANA) and used by Windows networking APIs. These types are commonly used
    /// when querying or configuring network interfaces, such as with Windows Management Instrumentation (WMI), SNMP, or
    /// low-level system calls. The enumeration covers a wide range of interface technologies, including legacy,
    /// physical, and virtual types. When interpreting these values, refer to official IANA documentation for detailed
    /// definitions and intended usage. Not all values may be supported or applicable on all systems.</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "There is no zero value for this in the Win32 API or IANA's specification.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "This enum is typed exactly as it's represented in the Win32 API.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "These are named as per the Win32 API and IANA's specification.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1712:Do not prefix enum values with type name", Justification = "These are named as per the Win32 API and IANA's specification.")]
    public enum IF_TYPE : uint
    {
        /// <summary>
        /// IF_TYPE_OTHER
        /// </summary>
        IF_TYPE_OTHER = Windows.Win32.PInvoke.IF_TYPE_OTHER,

        /// <summary>
        /// IF_TYPE_REGULAR_1822
        /// </summary>
        IF_TYPE_REGULAR_1822 = Windows.Win32.PInvoke.IF_TYPE_REGULAR_1822,

        /// <summary>
        /// IF_TYPE_HDH_1822
        /// </summary>
        IF_TYPE_HDH_1822 = Windows.Win32.PInvoke.IF_TYPE_HDH_1822,

        /// <summary>
        /// IF_TYPE_DDN_X25
        /// </summary>
        IF_TYPE_DDN_X25 = Windows.Win32.PInvoke.IF_TYPE_DDN_X25,

        /// <summary>
        /// IF_TYPE_RFC877_X25
        /// </summary>
        IF_TYPE_RFC877_X25 = Windows.Win32.PInvoke.IF_TYPE_RFC877_X25,

        /// <summary>
        /// IF_TYPE_ETHERNET_CSMACD
        /// </summary>
        IF_TYPE_ETHERNET_CSMACD = Windows.Win32.PInvoke.IF_TYPE_ETHERNET_CSMACD,

        /// <summary>
        /// IF_TYPE_IS088023_CSMACD
        /// </summary>
        IF_TYPE_IS088023_CSMACD = Windows.Win32.PInvoke.IF_TYPE_IS088023_CSMACD,

        /// <summary>
        /// IF_TYPE_ISO88024_TOKENBUS
        /// </summary>
        IF_TYPE_ISO88024_TOKENBUS = Windows.Win32.PInvoke.IF_TYPE_ISO88024_TOKENBUS,

        /// <summary>
        /// IF_TYPE_ISO88025_TOKENRING
        /// </summary>
        IF_TYPE_ISO88025_TOKENRING = Windows.Win32.PInvoke.IF_TYPE_ISO88025_TOKENRING,

        /// <summary>
        /// IF_TYPE_ISO88026_MAN
        /// </summary>
        IF_TYPE_ISO88026_MAN = Windows.Win32.PInvoke.IF_TYPE_ISO88026_MAN,

        /// <summary>
        /// IF_TYPE_STARLAN
        /// </summary>
        IF_TYPE_STARLAN = Windows.Win32.PInvoke.IF_TYPE_STARLAN,

        /// <summary>
        /// IF_TYPE_PROTEON_10MBIT
        /// </summary>
        IF_TYPE_PROTEON_10MBIT = Windows.Win32.PInvoke.IF_TYPE_PROTEON_10MBIT,

        /// <summary>
        /// IF_TYPE_PROTEON_80MBIT
        /// </summary>
        IF_TYPE_PROTEON_80MBIT = Windows.Win32.PInvoke.IF_TYPE_PROTEON_80MBIT,

        /// <summary>
        /// IF_TYPE_HYPERCHANNEL
        /// </summary>
        IF_TYPE_HYPERCHANNEL = Windows.Win32.PInvoke.IF_TYPE_HYPERCHANNEL,

        /// <summary>
        /// IF_TYPE_FDDI
        /// </summary>
        IF_TYPE_FDDI = Windows.Win32.PInvoke.IF_TYPE_FDDI,

        /// <summary>
        /// IF_TYPE_LAP_B
        /// </summary>
        IF_TYPE_LAP_B = Windows.Win32.PInvoke.IF_TYPE_LAP_B,

        /// <summary>
        /// IF_TYPE_SDLC
        /// </summary>
        IF_TYPE_SDLC = Windows.Win32.PInvoke.IF_TYPE_SDLC,

        /// <summary>
        /// IF_TYPE_DS1
        /// </summary>
        IF_TYPE_DS1 = Windows.Win32.PInvoke.IF_TYPE_DS1,

        /// <summary>
        /// IF_TYPE_E1
        /// </summary>
        IF_TYPE_E1 = Windows.Win32.PInvoke.IF_TYPE_E1,

        /// <summary>
        /// IF_TYPE_BASIC_ISDN
        /// </summary>
        IF_TYPE_BASIC_ISDN = Windows.Win32.PInvoke.IF_TYPE_BASIC_ISDN,

        /// <summary>
        /// IF_TYPE_PRIMARY_ISDN
        /// </summary>
        IF_TYPE_PRIMARY_ISDN = Windows.Win32.PInvoke.IF_TYPE_PRIMARY_ISDN,

        /// <summary>
        /// IF_TYPE_PROP_POINT2POINT_SERIAL
        /// </summary>
        IF_TYPE_PROP_POINT2POINT_SERIAL = Windows.Win32.PInvoke.IF_TYPE_PROP_POINT2POINT_SERIAL,

        /// <summary>
        /// IF_TYPE_PPP
        /// </summary>
        IF_TYPE_PPP = Windows.Win32.PInvoke.IF_TYPE_PPP,

        /// <summary>
        /// IF_TYPE_SOFTWARE_LOOPBACK
        /// </summary>
        IF_TYPE_SOFTWARE_LOOPBACK = Windows.Win32.PInvoke.IF_TYPE_SOFTWARE_LOOPBACK,

        /// <summary>
        /// IF_TYPE_EON
        /// </summary>
        IF_TYPE_EON = Windows.Win32.PInvoke.IF_TYPE_EON,

        /// <summary>
        /// IF_TYPE_ETHERNET_3MBIT
        /// </summary>
        IF_TYPE_ETHERNET_3MBIT = Windows.Win32.PInvoke.IF_TYPE_ETHERNET_3MBIT,

        /// <summary>
        /// IF_TYPE_NSIP
        /// </summary>
        IF_TYPE_NSIP = Windows.Win32.PInvoke.IF_TYPE_NSIP,

        /// <summary>
        /// IF_TYPE_SLIP
        /// </summary>
        IF_TYPE_SLIP = Windows.Win32.PInvoke.IF_TYPE_SLIP,

        /// <summary>
        /// IF_TYPE_ULTRA
        /// </summary>
        IF_TYPE_ULTRA = Windows.Win32.PInvoke.IF_TYPE_ULTRA,

        /// <summary>
        /// IF_TYPE_DS3
        /// </summary>
        IF_TYPE_DS3 = Windows.Win32.PInvoke.IF_TYPE_DS3,

        /// <summary>
        /// IF_TYPE_SIP
        /// </summary>
        IF_TYPE_SIP = Windows.Win32.PInvoke.IF_TYPE_SIP,

        /// <summary>
        /// IF_TYPE_FRAMERELAY
        /// </summary>
        IF_TYPE_FRAMERELAY = Windows.Win32.PInvoke.IF_TYPE_FRAMERELAY,

        /// <summary>
        /// IF_TYPE_RS232
        /// </summary>
        IF_TYPE_RS232 = Windows.Win32.PInvoke.IF_TYPE_RS232,

        /// <summary>
        /// IF_TYPE_PARA
        /// </summary>
        IF_TYPE_PARA = Windows.Win32.PInvoke.IF_TYPE_PARA,

        /// <summary>
        /// IF_TYPE_ARCNET
        /// </summary>
        IF_TYPE_ARCNET = Windows.Win32.PInvoke.IF_TYPE_ARCNET,

        /// <summary>
        /// IF_TYPE_ARCNET_PLUS
        /// </summary>
        IF_TYPE_ARCNET_PLUS = Windows.Win32.PInvoke.IF_TYPE_ARCNET_PLUS,

        /// <summary>
        /// IF_TYPE_ATM
        /// </summary>
        IF_TYPE_ATM = Windows.Win32.PInvoke.IF_TYPE_ATM,

        /// <summary>
        /// IF_TYPE_MIO_X25
        /// </summary>
        IF_TYPE_MIO_X25 = Windows.Win32.PInvoke.IF_TYPE_MIO_X25,

        /// <summary>
        /// IF_TYPE_SONET
        /// </summary>
        IF_TYPE_SONET = Windows.Win32.PInvoke.IF_TYPE_SONET,

        /// <summary>
        /// IF_TYPE_X25_PLE
        /// </summary>
        IF_TYPE_X25_PLE = Windows.Win32.PInvoke.IF_TYPE_X25_PLE,

        /// <summary>
        /// IF_TYPE_ISO88022_LLC
        /// </summary>
        IF_TYPE_ISO88022_LLC = Windows.Win32.PInvoke.IF_TYPE_ISO88022_LLC,

        /// <summary>
        /// IF_TYPE_LOCALTALK
        /// </summary>
        IF_TYPE_LOCALTALK = Windows.Win32.PInvoke.IF_TYPE_LOCALTALK,

        /// <summary>
        /// IF_TYPE_SMDS_DXI
        /// </summary>
        IF_TYPE_SMDS_DXI = Windows.Win32.PInvoke.IF_TYPE_SMDS_DXI,

        /// <summary>
        /// IF_TYPE_FRAMERELAY_SERVICE
        /// </summary>
        IF_TYPE_FRAMERELAY_SERVICE = Windows.Win32.PInvoke.IF_TYPE_FRAMERELAY_SERVICE,

        /// <summary>
        /// IF_TYPE_V35
        /// </summary>
        IF_TYPE_V35 = Windows.Win32.PInvoke.IF_TYPE_V35,

        /// <summary>
        /// IF_TYPE_HSSI
        /// </summary>
        IF_TYPE_HSSI = Windows.Win32.PInvoke.IF_TYPE_HSSI,

        /// <summary>
        /// IF_TYPE_HIPPI
        /// </summary>
        IF_TYPE_HIPPI = Windows.Win32.PInvoke.IF_TYPE_HIPPI,

        /// <summary>
        /// IF_TYPE_MODEM
        /// </summary>
        IF_TYPE_MODEM = Windows.Win32.PInvoke.IF_TYPE_MODEM,

        /// <summary>
        /// IF_TYPE_AAL5
        /// </summary>
        IF_TYPE_AAL5 = Windows.Win32.PInvoke.IF_TYPE_AAL5,

        /// <summary>
        /// IF_TYPE_SONET_PATH
        /// </summary>
        IF_TYPE_SONET_PATH = Windows.Win32.PInvoke.IF_TYPE_SONET_PATH,

        /// <summary>
        /// IF_TYPE_SONET_VT
        /// </summary>
        IF_TYPE_SONET_VT = Windows.Win32.PInvoke.IF_TYPE_SONET_VT,

        /// <summary>
        /// IF_TYPE_SMDS_ICIP
        /// </summary>
        IF_TYPE_SMDS_ICIP = Windows.Win32.PInvoke.IF_TYPE_SMDS_ICIP,

        /// <summary>
        /// IF_TYPE_PROP_VIRTUAL
        /// </summary>
        IF_TYPE_PROP_VIRTUAL = Windows.Win32.PInvoke.IF_TYPE_PROP_VIRTUAL,

        /// <summary>
        /// IF_TYPE_PROP_MULTIPLEXOR
        /// </summary>
        IF_TYPE_PROP_MULTIPLEXOR = Windows.Win32.PInvoke.IF_TYPE_PROP_MULTIPLEXOR,

        /// <summary>
        /// IF_TYPE_IEEE80212
        /// </summary>
        IF_TYPE_IEEE80212 = Windows.Win32.PInvoke.IF_TYPE_IEEE80212,

        /// <summary>
        /// IF_TYPE_FIBRECHANNEL
        /// </summary>
        IF_TYPE_FIBRECHANNEL = Windows.Win32.PInvoke.IF_TYPE_FIBRECHANNEL,

        /// <summary>
        /// IF_TYPE_HIPPIINTERFACE
        /// </summary>
        IF_TYPE_HIPPIINTERFACE = Windows.Win32.PInvoke.IF_TYPE_HIPPIINTERFACE,

        /// <summary>
        /// IF_TYPE_FRAMERELAY_INTERCONNECT
        /// </summary>
        IF_TYPE_FRAMERELAY_INTERCONNECT = Windows.Win32.PInvoke.IF_TYPE_FRAMERELAY_INTERCONNECT,

        /// <summary>
        /// IF_TYPE_AFLANE_8023
        /// </summary>
        IF_TYPE_AFLANE_8023 = Windows.Win32.PInvoke.IF_TYPE_AFLANE_8023,

        /// <summary>
        /// IF_TYPE_AFLANE_8025
        /// </summary>
        IF_TYPE_AFLANE_8025 = Windows.Win32.PInvoke.IF_TYPE_AFLANE_8025,

        /// <summary>
        /// IF_TYPE_CCTEMUL
        /// </summary>
        IF_TYPE_CCTEMUL = Windows.Win32.PInvoke.IF_TYPE_CCTEMUL,

        /// <summary>
        /// IF_TYPE_FASTETHER
        /// </summary>
        IF_TYPE_FASTETHER = Windows.Win32.PInvoke.IF_TYPE_FASTETHER,

        /// <summary>
        /// IF_TYPE_ISDN
        /// </summary>
        IF_TYPE_ISDN = Windows.Win32.PInvoke.IF_TYPE_ISDN,

        /// <summary>
        /// IF_TYPE_V11
        /// </summary>
        IF_TYPE_V11 = Windows.Win32.PInvoke.IF_TYPE_V11,

        /// <summary>
        /// IF_TYPE_V36
        /// </summary>
        IF_TYPE_V36 = Windows.Win32.PInvoke.IF_TYPE_V36,

        /// <summary>
        /// IF_TYPE_G703_64K
        /// </summary>
        IF_TYPE_G703_64K = Windows.Win32.PInvoke.IF_TYPE_G703_64K,

        /// <summary>
        /// IF_TYPE_G703_2MB
        /// </summary>
        IF_TYPE_G703_2MB = Windows.Win32.PInvoke.IF_TYPE_G703_2MB,

        /// <summary>
        /// IF_TYPE_QLLC
        /// </summary>
        IF_TYPE_QLLC = Windows.Win32.PInvoke.IF_TYPE_QLLC,

        /// <summary>
        /// IF_TYPE_FASTETHER_FX
        /// </summary>
        IF_TYPE_FASTETHER_FX = Windows.Win32.PInvoke.IF_TYPE_FASTETHER_FX,

        /// <summary>
        /// IF_TYPE_CHANNEL
        /// </summary>
        IF_TYPE_CHANNEL = Windows.Win32.PInvoke.IF_TYPE_CHANNEL,

        /// <summary>
        /// IF_TYPE_IEEE80211
        /// </summary>
        IF_TYPE_IEEE80211 = Windows.Win32.PInvoke.IF_TYPE_IEEE80211,

        /// <summary>
        /// IF_TYPE_IBM370PARCHAN
        /// </summary>
        IF_TYPE_IBM370PARCHAN = Windows.Win32.PInvoke.IF_TYPE_IBM370PARCHAN,

        /// <summary>
        /// IF_TYPE_ESCON
        /// </summary>
        IF_TYPE_ESCON = Windows.Win32.PInvoke.IF_TYPE_ESCON,

        /// <summary>
        /// IF_TYPE_DLSW
        /// </summary>
        IF_TYPE_DLSW = Windows.Win32.PInvoke.IF_TYPE_DLSW,

        /// <summary>
        /// IF_TYPE_ISDN_S
        /// </summary>
        IF_TYPE_ISDN_S = Windows.Win32.PInvoke.IF_TYPE_ISDN_S,

        /// <summary>
        /// IF_TYPE_ISDN_U
        /// </summary>
        IF_TYPE_ISDN_U = Windows.Win32.PInvoke.IF_TYPE_ISDN_U,

        /// <summary>
        /// IF_TYPE_LAP_D
        /// </summary>
        IF_TYPE_LAP_D = Windows.Win32.PInvoke.IF_TYPE_LAP_D,

        /// <summary>
        /// IF_TYPE_IPSWITCH
        /// </summary>
        IF_TYPE_IPSWITCH = Windows.Win32.PInvoke.IF_TYPE_IPSWITCH,

        /// <summary>
        /// IF_TYPE_RSRB
        /// </summary>
        IF_TYPE_RSRB = Windows.Win32.PInvoke.IF_TYPE_RSRB,

        /// <summary>
        /// IF_TYPE_ATM_LOGICAL
        /// </summary>
        IF_TYPE_ATM_LOGICAL = Windows.Win32.PInvoke.IF_TYPE_ATM_LOGICAL,

        /// <summary>
        /// IF_TYPE_DS0
        /// </summary>
        IF_TYPE_DS0 = Windows.Win32.PInvoke.IF_TYPE_DS0,

        /// <summary>
        /// IF_TYPE_DS0_BUNDLE
        /// </summary>
        IF_TYPE_DS0_BUNDLE = Windows.Win32.PInvoke.IF_TYPE_DS0_BUNDLE,

        /// <summary>
        /// IF_TYPE_BSC
        /// </summary>
        IF_TYPE_BSC = Windows.Win32.PInvoke.IF_TYPE_BSC,

        /// <summary>
        /// IF_TYPE_ASYNC
        /// </summary>
        IF_TYPE_ASYNC = Windows.Win32.PInvoke.IF_TYPE_ASYNC,

        /// <summary>
        /// IF_TYPE_CNR
        /// </summary>
        IF_TYPE_CNR = Windows.Win32.PInvoke.IF_TYPE_CNR,

        /// <summary>
        /// IF_TYPE_ISO88025R_DTR
        /// </summary>
        IF_TYPE_ISO88025R_DTR = Windows.Win32.PInvoke.IF_TYPE_ISO88025R_DTR,

        /// <summary>
        /// IF_TYPE_EPLRS
        /// </summary>
        IF_TYPE_EPLRS = Windows.Win32.PInvoke.IF_TYPE_EPLRS,

        /// <summary>
        /// IF_TYPE_ARAP
        /// </summary>
        IF_TYPE_ARAP = Windows.Win32.PInvoke.IF_TYPE_ARAP,

        /// <summary>
        /// IF_TYPE_PROP_CNLS
        /// </summary>
        IF_TYPE_PROP_CNLS = Windows.Win32.PInvoke.IF_TYPE_PROP_CNLS,

        /// <summary>
        /// IF_TYPE_HOSTPAD
        /// </summary>
        IF_TYPE_HOSTPAD = Windows.Win32.PInvoke.IF_TYPE_HOSTPAD,

        /// <summary>
        /// IF_TYPE_TERMPAD
        /// </summary>
        IF_TYPE_TERMPAD = Windows.Win32.PInvoke.IF_TYPE_TERMPAD,

        /// <summary>
        /// IF_TYPE_FRAMERELAY_MPI
        /// </summary>
        IF_TYPE_FRAMERELAY_MPI = Windows.Win32.PInvoke.IF_TYPE_FRAMERELAY_MPI,

        /// <summary>
        /// IF_TYPE_X213
        /// </summary>
        IF_TYPE_X213 = Windows.Win32.PInvoke.IF_TYPE_X213,

        /// <summary>
        /// IF_TYPE_ADSL
        /// </summary>
        IF_TYPE_ADSL = Windows.Win32.PInvoke.IF_TYPE_ADSL,

        /// <summary>
        /// IF_TYPE_RADSL
        /// </summary>
        IF_TYPE_RADSL = Windows.Win32.PInvoke.IF_TYPE_RADSL,

        /// <summary>
        /// IF_TYPE_SDSL
        /// </summary>
        IF_TYPE_SDSL = Windows.Win32.PInvoke.IF_TYPE_SDSL,

        /// <summary>
        /// IF_TYPE_VDSL
        /// </summary>
        IF_TYPE_VDSL = Windows.Win32.PInvoke.IF_TYPE_VDSL,

        /// <summary>
        /// IF_TYPE_ISO88025_CRFPRINT
        /// </summary>
        IF_TYPE_ISO88025_CRFPRINT = Windows.Win32.PInvoke.IF_TYPE_ISO88025_CRFPRINT,

        /// <summary>
        /// IF_TYPE_MYRINET
        /// </summary>
        IF_TYPE_MYRINET = Windows.Win32.PInvoke.IF_TYPE_MYRINET,

        /// <summary>
        /// IF_TYPE_VOICE_EM
        /// </summary>
        IF_TYPE_VOICE_EM = Windows.Win32.PInvoke.IF_TYPE_VOICE_EM,

        /// <summary>
        /// IF_TYPE_VOICE_FXO
        /// </summary>
        IF_TYPE_VOICE_FXO = Windows.Win32.PInvoke.IF_TYPE_VOICE_FXO,

        /// <summary>
        /// IF_TYPE_VOICE_FXS
        /// </summary>
        IF_TYPE_VOICE_FXS = Windows.Win32.PInvoke.IF_TYPE_VOICE_FXS,

        /// <summary>
        /// IF_TYPE_VOICE_ENCAP
        /// </summary>
        IF_TYPE_VOICE_ENCAP = Windows.Win32.PInvoke.IF_TYPE_VOICE_ENCAP,

        /// <summary>
        /// IF_TYPE_VOICE_OVERIP
        /// </summary>
        IF_TYPE_VOICE_OVERIP = Windows.Win32.PInvoke.IF_TYPE_VOICE_OVERIP,

        /// <summary>
        /// IF_TYPE_ATM_DXI
        /// </summary>
        IF_TYPE_ATM_DXI = Windows.Win32.PInvoke.IF_TYPE_ATM_DXI,

        /// <summary>
        /// IF_TYPE_ATM_FUNI
        /// </summary>
        IF_TYPE_ATM_FUNI = Windows.Win32.PInvoke.IF_TYPE_ATM_FUNI,

        /// <summary>
        /// IF_TYPE_ATM_IMA
        /// </summary>
        IF_TYPE_ATM_IMA = Windows.Win32.PInvoke.IF_TYPE_ATM_IMA,

        /// <summary>
        /// IF_TYPE_PPPMULTILINKBUNDLE
        /// </summary>
        IF_TYPE_PPPMULTILINKBUNDLE = Windows.Win32.PInvoke.IF_TYPE_PPPMULTILINKBUNDLE,

        /// <summary>
        /// IF_TYPE_IPOVER_CDLC
        /// </summary>
        IF_TYPE_IPOVER_CDLC = Windows.Win32.PInvoke.IF_TYPE_IPOVER_CDLC,

        /// <summary>
        /// IF_TYPE_IPOVER_CLAW
        /// </summary>
        IF_TYPE_IPOVER_CLAW = Windows.Win32.PInvoke.IF_TYPE_IPOVER_CLAW,

        /// <summary>
        /// IF_TYPE_STACKTOSTACK
        /// </summary>
        IF_TYPE_STACKTOSTACK = Windows.Win32.PInvoke.IF_TYPE_STACKTOSTACK,

        /// <summary>
        /// IF_TYPE_VIRTUALIPADDRESS
        /// </summary>
        IF_TYPE_VIRTUALIPADDRESS = Windows.Win32.PInvoke.IF_TYPE_VIRTUALIPADDRESS,

        /// <summary>
        /// IF_TYPE_MPC
        /// </summary>
        IF_TYPE_MPC = Windows.Win32.PInvoke.IF_TYPE_MPC,

        /// <summary>
        /// IF_TYPE_IPOVER_ATM
        /// </summary>
        IF_TYPE_IPOVER_ATM = Windows.Win32.PInvoke.IF_TYPE_IPOVER_ATM,

        /// <summary>
        /// IF_TYPE_ISO88025_FIBER
        /// </summary>
        IF_TYPE_ISO88025_FIBER = Windows.Win32.PInvoke.IF_TYPE_ISO88025_FIBER,

        /// <summary>
        /// IF_TYPE_TDLC
        /// </summary>
        IF_TYPE_TDLC = Windows.Win32.PInvoke.IF_TYPE_TDLC,

        /// <summary>
        /// IF_TYPE_GIGABITETHERNET
        /// </summary>
        IF_TYPE_GIGABITETHERNET = Windows.Win32.PInvoke.IF_TYPE_GIGABITETHERNET,

        /// <summary>
        /// IF_TYPE_HDLC
        /// </summary>
        IF_TYPE_HDLC = Windows.Win32.PInvoke.IF_TYPE_HDLC,

        /// <summary>
        /// IF_TYPE_LAP_F
        /// </summary>
        IF_TYPE_LAP_F = Windows.Win32.PInvoke.IF_TYPE_LAP_F,

        /// <summary>
        /// IF_TYPE_V37
        /// </summary>
        IF_TYPE_V37 = Windows.Win32.PInvoke.IF_TYPE_V37,

        /// <summary>
        /// IF_TYPE_X25_MLP
        /// </summary>
        IF_TYPE_X25_MLP = Windows.Win32.PInvoke.IF_TYPE_X25_MLP,

        /// <summary>
        /// IF_TYPE_X25_HUNTGROUP
        /// </summary>
        IF_TYPE_X25_HUNTGROUP = Windows.Win32.PInvoke.IF_TYPE_X25_HUNTGROUP,

        /// <summary>
        /// IF_TYPE_TRANSPHDLC
        /// </summary>
        IF_TYPE_TRANSPHDLC = Windows.Win32.PInvoke.IF_TYPE_TRANSPHDLC,

        /// <summary>
        /// IF_TYPE_INTERLEAVE
        /// </summary>
        IF_TYPE_INTERLEAVE = Windows.Win32.PInvoke.IF_TYPE_INTERLEAVE,

        /// <summary>
        /// IF_TYPE_FAST
        /// </summary>
        IF_TYPE_FAST = Windows.Win32.PInvoke.IF_TYPE_FAST,

        /// <summary>
        /// IF_TYPE_IP
        /// </summary>
        IF_TYPE_IP = Windows.Win32.PInvoke.IF_TYPE_IP,

        /// <summary>
        /// IF_TYPE_DOCSCABLE_MACLAYER
        /// </summary>
        IF_TYPE_DOCSCABLE_MACLAYER = Windows.Win32.PInvoke.IF_TYPE_DOCSCABLE_MACLAYER,

        /// <summary>
        /// IF_TYPE_DOCSCABLE_DOWNSTREAM
        /// </summary>
        IF_TYPE_DOCSCABLE_DOWNSTREAM = Windows.Win32.PInvoke.IF_TYPE_DOCSCABLE_DOWNSTREAM,

        /// <summary>
        /// IF_TYPE_DOCSCABLE_UPSTREAM
        /// </summary>
        IF_TYPE_DOCSCABLE_UPSTREAM = Windows.Win32.PInvoke.IF_TYPE_DOCSCABLE_UPSTREAM,

        /// <summary>
        /// IF_TYPE_A12MPPSWITCH
        /// </summary>
        IF_TYPE_A12MPPSWITCH = Windows.Win32.PInvoke.IF_TYPE_A12MPPSWITCH,

        /// <summary>
        /// IF_TYPE_TUNNEL
        /// </summary>
        IF_TYPE_TUNNEL = Windows.Win32.PInvoke.IF_TYPE_TUNNEL,

        /// <summary>
        /// IF_TYPE_COFFEE
        /// </summary>
        IF_TYPE_COFFEE = Windows.Win32.PInvoke.IF_TYPE_COFFEE,

        /// <summary>
        /// IF_TYPE_CES
        /// </summary>
        IF_TYPE_CES = Windows.Win32.PInvoke.IF_TYPE_CES,

        /// <summary>
        /// IF_TYPE_ATM_SUBINTERFACE
        /// </summary>
        IF_TYPE_ATM_SUBINTERFACE = Windows.Win32.PInvoke.IF_TYPE_ATM_SUBINTERFACE,

        /// <summary>
        /// IF_TYPE_L2_VLAN
        /// </summary>
        IF_TYPE_L2_VLAN = Windows.Win32.PInvoke.IF_TYPE_L2_VLAN,

        /// <summary>
        /// IF_TYPE_L3_IPVLAN
        /// </summary>
        IF_TYPE_L3_IPVLAN = Windows.Win32.PInvoke.IF_TYPE_L3_IPVLAN,

        /// <summary>
        /// IF_TYPE_L3_IPXVLAN
        /// </summary>
        IF_TYPE_L3_IPXVLAN = Windows.Win32.PInvoke.IF_TYPE_L3_IPXVLAN,

        /// <summary>
        /// IF_TYPE_DIGITALPOWERLINE
        /// </summary>
        IF_TYPE_DIGITALPOWERLINE = Windows.Win32.PInvoke.IF_TYPE_DIGITALPOWERLINE,

        /// <summary>
        /// IF_TYPE_MEDIAMAILOVERIP
        /// </summary>
        IF_TYPE_MEDIAMAILOVERIP = Windows.Win32.PInvoke.IF_TYPE_MEDIAMAILOVERIP,

        /// <summary>
        /// IF_TYPE_DTM
        /// </summary>
        IF_TYPE_DTM = Windows.Win32.PInvoke.IF_TYPE_DTM,

        /// <summary>
        /// IF_TYPE_DCN
        /// </summary>
        IF_TYPE_DCN = Windows.Win32.PInvoke.IF_TYPE_DCN,

        /// <summary>
        /// IF_TYPE_IPFORWARD
        /// </summary>
        IF_TYPE_IPFORWARD = Windows.Win32.PInvoke.IF_TYPE_IPFORWARD,

        /// <summary>
        /// IF_TYPE_MSDSL
        /// </summary>
        IF_TYPE_MSDSL = Windows.Win32.PInvoke.IF_TYPE_MSDSL,

        /// <summary>
        /// IF_TYPE_IEEE1394
        /// </summary>
        IF_TYPE_IEEE1394 = Windows.Win32.PInvoke.IF_TYPE_IEEE1394,

        /// <summary>
        /// IF_TYPE_IF_GSN
        /// </summary>
        IF_TYPE_IF_GSN = Windows.Win32.PInvoke.IF_TYPE_IF_GSN,

        /// <summary>
        /// IF_TYPE_DVBRCC_MACLAYER
        /// </summary>
        IF_TYPE_DVBRCC_MACLAYER = Windows.Win32.PInvoke.IF_TYPE_DVBRCC_MACLAYER,

        /// <summary>
        /// IF_TYPE_DVBRCC_DOWNSTREAM
        /// </summary>
        IF_TYPE_DVBRCC_DOWNSTREAM = Windows.Win32.PInvoke.IF_TYPE_DVBRCC_DOWNSTREAM,

        /// <summary>
        /// IF_TYPE_DVBRCC_UPSTREAM
        /// </summary>
        IF_TYPE_DVBRCC_UPSTREAM = Windows.Win32.PInvoke.IF_TYPE_DVBRCC_UPSTREAM,

        /// <summary>
        /// IF_TYPE_ATM_VIRTUAL
        /// </summary>
        IF_TYPE_ATM_VIRTUAL = Windows.Win32.PInvoke.IF_TYPE_ATM_VIRTUAL,

        /// <summary>
        /// IF_TYPE_MPLS_TUNNEL
        /// </summary>
        IF_TYPE_MPLS_TUNNEL = Windows.Win32.PInvoke.IF_TYPE_MPLS_TUNNEL,

        /// <summary>
        /// IF_TYPE_SRP
        /// </summary>
        IF_TYPE_SRP = Windows.Win32.PInvoke.IF_TYPE_SRP,

        /// <summary>
        /// IF_TYPE_VOICEOVERATM
        /// </summary>
        IF_TYPE_VOICEOVERATM = Windows.Win32.PInvoke.IF_TYPE_VOICEOVERATM,

        /// <summary>
        /// IF_TYPE_VOICEOVERFRAMERELAY
        /// </summary>
        IF_TYPE_VOICEOVERFRAMERELAY = Windows.Win32.PInvoke.IF_TYPE_VOICEOVERFRAMERELAY,

        /// <summary>
        /// IF_TYPE_IDSL
        /// </summary>
        IF_TYPE_IDSL = Windows.Win32.PInvoke.IF_TYPE_IDSL,

        /// <summary>
        /// IF_TYPE_COMPOSITELINK
        /// </summary>
        IF_TYPE_COMPOSITELINK = Windows.Win32.PInvoke.IF_TYPE_COMPOSITELINK,

        /// <summary>
        /// IF_TYPE_SS7_SIGLINK
        /// </summary>
        IF_TYPE_SS7_SIGLINK = Windows.Win32.PInvoke.IF_TYPE_SS7_SIGLINK,

        /// <summary>
        /// IF_TYPE_PROP_WIRELESS_P2P
        /// </summary>
        IF_TYPE_PROP_WIRELESS_P2P = Windows.Win32.PInvoke.IF_TYPE_PROP_WIRELESS_P2P,

        /// <summary>
        /// IF_TYPE_FR_FORWARD
        /// </summary>
        IF_TYPE_FR_FORWARD = Windows.Win32.PInvoke.IF_TYPE_FR_FORWARD,

        /// <summary>
        /// IF_TYPE_RFC1483
        /// </summary>
        IF_TYPE_RFC1483 = Windows.Win32.PInvoke.IF_TYPE_RFC1483,

        /// <summary>
        /// IF_TYPE_USB
        /// </summary>
        IF_TYPE_USB = Windows.Win32.PInvoke.IF_TYPE_USB,

        /// <summary>
        /// IF_TYPE_IEEE8023AD_LAG
        /// </summary>
        IF_TYPE_IEEE8023AD_LAG = Windows.Win32.PInvoke.IF_TYPE_IEEE8023AD_LAG,

        /// <summary>
        /// IF_TYPE_BGP_POLICY_ACCOUNTING
        /// </summary>
        IF_TYPE_BGP_POLICY_ACCOUNTING = Windows.Win32.PInvoke.IF_TYPE_BGP_POLICY_ACCOUNTING,

        /// <summary>
        /// IF_TYPE_FRF16_MFR_BUNDLE
        /// </summary>
        IF_TYPE_FRF16_MFR_BUNDLE = Windows.Win32.PInvoke.IF_TYPE_FRF16_MFR_BUNDLE,

        /// <summary>
        /// IF_TYPE_H323_GATEKEEPER
        /// </summary>
        IF_TYPE_H323_GATEKEEPER = Windows.Win32.PInvoke.IF_TYPE_H323_GATEKEEPER,

        /// <summary>
        /// IF_TYPE_H323_PROXY
        /// </summary>
        IF_TYPE_H323_PROXY = Windows.Win32.PInvoke.IF_TYPE_H323_PROXY,

        /// <summary>
        /// IF_TYPE_MPLS
        /// </summary>
        IF_TYPE_MPLS = Windows.Win32.PInvoke.IF_TYPE_MPLS,

        /// <summary>
        /// IF_TYPE_MF_SIGLINK
        /// </summary>
        IF_TYPE_MF_SIGLINK = Windows.Win32.PInvoke.IF_TYPE_MF_SIGLINK,

        /// <summary>
        /// IF_TYPE_HDSL2
        /// </summary>
        IF_TYPE_HDSL2 = Windows.Win32.PInvoke.IF_TYPE_HDSL2,

        /// <summary>
        /// IF_TYPE_SHDSL
        /// </summary>
        IF_TYPE_SHDSL = Windows.Win32.PInvoke.IF_TYPE_SHDSL,

        /// <summary>
        /// IF_TYPE_DS1_FDL
        /// </summary>
        IF_TYPE_DS1_FDL = Windows.Win32.PInvoke.IF_TYPE_DS1_FDL,

        /// <summary>
        /// IF_TYPE_POS
        /// </summary>
        IF_TYPE_POS = Windows.Win32.PInvoke.IF_TYPE_POS,

        /// <summary>
        /// IF_TYPE_DVB_ASI_IN
        /// </summary>
        IF_TYPE_DVB_ASI_IN = Windows.Win32.PInvoke.IF_TYPE_DVB_ASI_IN,

        /// <summary>
        /// IF_TYPE_DVB_ASI_OUT
        /// </summary>
        IF_TYPE_DVB_ASI_OUT = Windows.Win32.PInvoke.IF_TYPE_DVB_ASI_OUT,

        /// <summary>
        /// IF_TYPE_PLC
        /// </summary>
        IF_TYPE_PLC = Windows.Win32.PInvoke.IF_TYPE_PLC,

        /// <summary>
        /// IF_TYPE_NFAS
        /// </summary>
        IF_TYPE_NFAS = Windows.Win32.PInvoke.IF_TYPE_NFAS,

        /// <summary>
        /// IF_TYPE_TR008
        /// </summary>
        IF_TYPE_TR008 = Windows.Win32.PInvoke.IF_TYPE_TR008,

        /// <summary>
        /// IF_TYPE_GR303_RDT
        /// </summary>
        IF_TYPE_GR303_RDT = Windows.Win32.PInvoke.IF_TYPE_GR303_RDT,

        /// <summary>
        /// IF_TYPE_GR303_IDT
        /// </summary>
        IF_TYPE_GR303_IDT = Windows.Win32.PInvoke.IF_TYPE_GR303_IDT,

        /// <summary>
        /// IF_TYPE_ISUP
        /// </summary>
        IF_TYPE_ISUP = Windows.Win32.PInvoke.IF_TYPE_ISUP,

        /// <summary>
        /// IF_TYPE_PROP_DOCS_WIRELESS_MACLAYER
        /// </summary>
        IF_TYPE_PROP_DOCS_WIRELESS_MACLAYER = Windows.Win32.PInvoke.IF_TYPE_PROP_DOCS_WIRELESS_MACLAYER,

        /// <summary>
        /// IF_TYPE_PROP_DOCS_WIRELESS_DOWNSTREAM
        /// </summary>
        IF_TYPE_PROP_DOCS_WIRELESS_DOWNSTREAM = Windows.Win32.PInvoke.IF_TYPE_PROP_DOCS_WIRELESS_DOWNSTREAM,

        /// <summary>
        /// IF_TYPE_PROP_DOCS_WIRELESS_UPSTREAM
        /// </summary>
        IF_TYPE_PROP_DOCS_WIRELESS_UPSTREAM = Windows.Win32.PInvoke.IF_TYPE_PROP_DOCS_WIRELESS_UPSTREAM,

        /// <summary>
        /// IF_TYPE_HIPERLAN2
        /// </summary>
        IF_TYPE_HIPERLAN2 = Windows.Win32.PInvoke.IF_TYPE_HIPERLAN2,

        /// <summary>
        /// IF_TYPE_PROP_BWA_P2MP
        /// </summary>
        IF_TYPE_PROP_BWA_P2MP = Windows.Win32.PInvoke.IF_TYPE_PROP_BWA_P2MP,

        /// <summary>
        /// IF_TYPE_SONET_OVERHEAD_CHANNEL
        /// </summary>
        IF_TYPE_SONET_OVERHEAD_CHANNEL = Windows.Win32.PInvoke.IF_TYPE_SONET_OVERHEAD_CHANNEL,

        /// <summary>
        /// IF_TYPE_DIGITAL_WRAPPER_OVERHEAD_CHANNEL
        /// </summary>
        IF_TYPE_DIGITAL_WRAPPER_OVERHEAD_CHANNEL = Windows.Win32.PInvoke.IF_TYPE_DIGITAL_WRAPPER_OVERHEAD_CHANNEL,

        /// <summary>
        /// IF_TYPE_AAL2
        /// </summary>
        IF_TYPE_AAL2 = Windows.Win32.PInvoke.IF_TYPE_AAL2,

        /// <summary>
        /// IF_TYPE_RADIO_MAC
        /// </summary>
        IF_TYPE_RADIO_MAC = Windows.Win32.PInvoke.IF_TYPE_RADIO_MAC,

        /// <summary>
        /// IF_TYPE_ATM_RADIO
        /// </summary>
        IF_TYPE_ATM_RADIO = Windows.Win32.PInvoke.IF_TYPE_ATM_RADIO,

        /// <summary>
        /// IF_TYPE_IMT
        /// </summary>
        IF_TYPE_IMT = Windows.Win32.PInvoke.IF_TYPE_IMT,

        /// <summary>
        /// IF_TYPE_MVL
        /// </summary>
        IF_TYPE_MVL = Windows.Win32.PInvoke.IF_TYPE_MVL,

        /// <summary>
        /// IF_TYPE_REACH_DSL
        /// </summary>
        IF_TYPE_REACH_DSL = Windows.Win32.PInvoke.IF_TYPE_REACH_DSL,

        /// <summary>
        /// IF_TYPE_FR_DLCI_ENDPT
        /// </summary>
        IF_TYPE_FR_DLCI_ENDPT = Windows.Win32.PInvoke.IF_TYPE_FR_DLCI_ENDPT,

        /// <summary>
        /// IF_TYPE_ATM_VCI_ENDPT
        /// </summary>
        IF_TYPE_ATM_VCI_ENDPT = Windows.Win32.PInvoke.IF_TYPE_ATM_VCI_ENDPT,

        /// <summary>
        /// IF_TYPE_OPTICAL_CHANNEL
        /// </summary>
        IF_TYPE_OPTICAL_CHANNEL = Windows.Win32.PInvoke.IF_TYPE_OPTICAL_CHANNEL,

        /// <summary>
        /// IF_TYPE_OPTICAL_TRANSPORT
        /// </summary>
        IF_TYPE_OPTICAL_TRANSPORT = Windows.Win32.PInvoke.IF_TYPE_OPTICAL_TRANSPORT,

        /// <summary>
        /// IF_TYPE_IEEE80216_WMAN
        /// </summary>
        IF_TYPE_IEEE80216_WMAN = Windows.Win32.PInvoke.IF_TYPE_IEEE80216_WMAN,

        /// <summary>
        /// IF_TYPE_WWANPP
        /// </summary>
        IF_TYPE_WWANPP = Windows.Win32.PInvoke.IF_TYPE_WWANPP,

        /// <summary>
        /// IF_TYPE_WWANPP2
        /// </summary>
        IF_TYPE_WWANPP2 = Windows.Win32.PInvoke.IF_TYPE_WWANPP2,

        /// <summary>
        /// IF_TYPE_IEEE802154
        /// </summary>
        IF_TYPE_IEEE802154 = Windows.Win32.PInvoke.IF_TYPE_IEEE802154,

        /// <summary>
        /// IF_TYPE_XBOX_WIRELESS
        /// </summary>
        IF_TYPE_XBOX_WIRELESS = Windows.Win32.PInvoke.IF_TYPE_XBOX_WIRELESS,
    }
}
