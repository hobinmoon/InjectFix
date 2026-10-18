using IFixBench.Target;

//Regression: an 8-byte constant (ldc.i8) right before an extern call. The low 32 bits cover 0~199,
//so whatever value ShuffleInstruction gives Constrained (fewer than 200 opcodes), one of them collides
#if IFIX_INTERP
namespace IFixBench.Interp
#else
namespace IFixBench.Native
#endif
{
    public static class ConstCases
    {
        public static int CheckCount()
        {
            return 1;
        }

        public static string Check(int id)
        {
            return "ldc.i8 before extern call: " + ConstantBeforeCall();
        }

        static long ConstantBeforeCall()
        {
            long r = 0;
            r += Api.Id64(0x100000000L);
            r += Api.Id64(0x100000001L);
            r += Api.Id64(0x100000002L);
            r += Api.Id64(0x100000003L);
            r += Api.Id64(0x100000004L);
            r += Api.Id64(0x100000005L);
            r += Api.Id64(0x100000006L);
            r += Api.Id64(0x100000007L);
            r += Api.Id64(0x100000008L);
            r += Api.Id64(0x100000009L);
            r += Api.Id64(0x10000000AL);
            r += Api.Id64(0x10000000BL);
            r += Api.Id64(0x10000000CL);
            r += Api.Id64(0x10000000DL);
            r += Api.Id64(0x10000000EL);
            r += Api.Id64(0x10000000FL);
            r += Api.Id64(0x100000010L);
            r += Api.Id64(0x100000011L);
            r += Api.Id64(0x100000012L);
            r += Api.Id64(0x100000013L);
            r += Api.Id64(0x100000014L);
            r += Api.Id64(0x100000015L);
            r += Api.Id64(0x100000016L);
            r += Api.Id64(0x100000017L);
            r += Api.Id64(0x100000018L);
            r += Api.Id64(0x100000019L);
            r += Api.Id64(0x10000001AL);
            r += Api.Id64(0x10000001BL);
            r += Api.Id64(0x10000001CL);
            r += Api.Id64(0x10000001DL);
            r += Api.Id64(0x10000001EL);
            r += Api.Id64(0x10000001FL);
            r += Api.Id64(0x100000020L);
            r += Api.Id64(0x100000021L);
            r += Api.Id64(0x100000022L);
            r += Api.Id64(0x100000023L);
            r += Api.Id64(0x100000024L);
            r += Api.Id64(0x100000025L);
            r += Api.Id64(0x100000026L);
            r += Api.Id64(0x100000027L);
            r += Api.Id64(0x100000028L);
            r += Api.Id64(0x100000029L);
            r += Api.Id64(0x10000002AL);
            r += Api.Id64(0x10000002BL);
            r += Api.Id64(0x10000002CL);
            r += Api.Id64(0x10000002DL);
            r += Api.Id64(0x10000002EL);
            r += Api.Id64(0x10000002FL);
            r += Api.Id64(0x100000030L);
            r += Api.Id64(0x100000031L);
            r += Api.Id64(0x100000032L);
            r += Api.Id64(0x100000033L);
            r += Api.Id64(0x100000034L);
            r += Api.Id64(0x100000035L);
            r += Api.Id64(0x100000036L);
            r += Api.Id64(0x100000037L);
            r += Api.Id64(0x100000038L);
            r += Api.Id64(0x100000039L);
            r += Api.Id64(0x10000003AL);
            r += Api.Id64(0x10000003BL);
            r += Api.Id64(0x10000003CL);
            r += Api.Id64(0x10000003DL);
            r += Api.Id64(0x10000003EL);
            r += Api.Id64(0x10000003FL);
            r += Api.Id64(0x100000040L);
            r += Api.Id64(0x100000041L);
            r += Api.Id64(0x100000042L);
            r += Api.Id64(0x100000043L);
            r += Api.Id64(0x100000044L);
            r += Api.Id64(0x100000045L);
            r += Api.Id64(0x100000046L);
            r += Api.Id64(0x100000047L);
            r += Api.Id64(0x100000048L);
            r += Api.Id64(0x100000049L);
            r += Api.Id64(0x10000004AL);
            r += Api.Id64(0x10000004BL);
            r += Api.Id64(0x10000004CL);
            r += Api.Id64(0x10000004DL);
            r += Api.Id64(0x10000004EL);
            r += Api.Id64(0x10000004FL);
            r += Api.Id64(0x100000050L);
            r += Api.Id64(0x100000051L);
            r += Api.Id64(0x100000052L);
            r += Api.Id64(0x100000053L);
            r += Api.Id64(0x100000054L);
            r += Api.Id64(0x100000055L);
            r += Api.Id64(0x100000056L);
            r += Api.Id64(0x100000057L);
            r += Api.Id64(0x100000058L);
            r += Api.Id64(0x100000059L);
            r += Api.Id64(0x10000005AL);
            r += Api.Id64(0x10000005BL);
            r += Api.Id64(0x10000005CL);
            r += Api.Id64(0x10000005DL);
            r += Api.Id64(0x10000005EL);
            r += Api.Id64(0x10000005FL);
            r += Api.Id64(0x100000060L);
            r += Api.Id64(0x100000061L);
            r += Api.Id64(0x100000062L);
            r += Api.Id64(0x100000063L);
            r += Api.Id64(0x100000064L);
            r += Api.Id64(0x100000065L);
            r += Api.Id64(0x100000066L);
            r += Api.Id64(0x100000067L);
            r += Api.Id64(0x100000068L);
            r += Api.Id64(0x100000069L);
            r += Api.Id64(0x10000006AL);
            r += Api.Id64(0x10000006BL);
            r += Api.Id64(0x10000006CL);
            r += Api.Id64(0x10000006DL);
            r += Api.Id64(0x10000006EL);
            r += Api.Id64(0x10000006FL);
            r += Api.Id64(0x100000070L);
            r += Api.Id64(0x100000071L);
            r += Api.Id64(0x100000072L);
            r += Api.Id64(0x100000073L);
            r += Api.Id64(0x100000074L);
            r += Api.Id64(0x100000075L);
            r += Api.Id64(0x100000076L);
            r += Api.Id64(0x100000077L);
            r += Api.Id64(0x100000078L);
            r += Api.Id64(0x100000079L);
            r += Api.Id64(0x10000007AL);
            r += Api.Id64(0x10000007BL);
            r += Api.Id64(0x10000007CL);
            r += Api.Id64(0x10000007DL);
            r += Api.Id64(0x10000007EL);
            r += Api.Id64(0x10000007FL);
            r += Api.Id64(0x100000080L);
            r += Api.Id64(0x100000081L);
            r += Api.Id64(0x100000082L);
            r += Api.Id64(0x100000083L);
            r += Api.Id64(0x100000084L);
            r += Api.Id64(0x100000085L);
            r += Api.Id64(0x100000086L);
            r += Api.Id64(0x100000087L);
            r += Api.Id64(0x100000088L);
            r += Api.Id64(0x100000089L);
            r += Api.Id64(0x10000008AL);
            r += Api.Id64(0x10000008BL);
            r += Api.Id64(0x10000008CL);
            r += Api.Id64(0x10000008DL);
            r += Api.Id64(0x10000008EL);
            r += Api.Id64(0x10000008FL);
            r += Api.Id64(0x100000090L);
            r += Api.Id64(0x100000091L);
            r += Api.Id64(0x100000092L);
            r += Api.Id64(0x100000093L);
            r += Api.Id64(0x100000094L);
            r += Api.Id64(0x100000095L);
            r += Api.Id64(0x100000096L);
            r += Api.Id64(0x100000097L);
            r += Api.Id64(0x100000098L);
            r += Api.Id64(0x100000099L);
            r += Api.Id64(0x10000009AL);
            r += Api.Id64(0x10000009BL);
            r += Api.Id64(0x10000009CL);
            r += Api.Id64(0x10000009DL);
            r += Api.Id64(0x10000009EL);
            r += Api.Id64(0x10000009FL);
            r += Api.Id64(0x1000000A0L);
            r += Api.Id64(0x1000000A1L);
            r += Api.Id64(0x1000000A2L);
            r += Api.Id64(0x1000000A3L);
            r += Api.Id64(0x1000000A4L);
            r += Api.Id64(0x1000000A5L);
            r += Api.Id64(0x1000000A6L);
            r += Api.Id64(0x1000000A7L);
            r += Api.Id64(0x1000000A8L);
            r += Api.Id64(0x1000000A9L);
            r += Api.Id64(0x1000000AAL);
            r += Api.Id64(0x1000000ABL);
            r += Api.Id64(0x1000000ACL);
            r += Api.Id64(0x1000000ADL);
            r += Api.Id64(0x1000000AEL);
            r += Api.Id64(0x1000000AFL);
            r += Api.Id64(0x1000000B0L);
            r += Api.Id64(0x1000000B1L);
            r += Api.Id64(0x1000000B2L);
            r += Api.Id64(0x1000000B3L);
            r += Api.Id64(0x1000000B4L);
            r += Api.Id64(0x1000000B5L);
            r += Api.Id64(0x1000000B6L);
            r += Api.Id64(0x1000000B7L);
            r += Api.Id64(0x1000000B8L);
            r += Api.Id64(0x1000000B9L);
            r += Api.Id64(0x1000000BAL);
            r += Api.Id64(0x1000000BBL);
            r += Api.Id64(0x1000000BCL);
            r += Api.Id64(0x1000000BDL);
            r += Api.Id64(0x1000000BEL);
            r += Api.Id64(0x1000000BFL);
            r += Api.Id64(0x1000000C0L);
            r += Api.Id64(0x1000000C1L);
            r += Api.Id64(0x1000000C2L);
            r += Api.Id64(0x1000000C3L);
            r += Api.Id64(0x1000000C4L);
            r += Api.Id64(0x1000000C5L);
            r += Api.Id64(0x1000000C6L);
            r += Api.Id64(0x1000000C7L);
            return r;
        }
    }
}
