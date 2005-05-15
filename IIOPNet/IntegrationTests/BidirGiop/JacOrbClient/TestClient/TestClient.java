import junit.framework.*;
import org.omg.CosNaming.*;
import org.omg.PortableServer.*;
import org.omg.BiDirPolicy.*;
import org.omg.CORBA.*;
import java.util.Properties;


/**
 * Integration test for IIOP.NET.
 *
 */
public class TestClient extends TestCase {

    private static TestService s_testService;
    private static org.omg.CORBA.ORB s_orb;
    private static CallBack s_cb;
    private static ClientCallbackImpl s_cbServant;
    private static CallbackIntIncrementer s_cbIntIncr;

    public static void main (String[] args) {
        try {
            Properties props = new Properties();
            props.put( "org.omg.PortableInterceptor.ORBInitializerClass.bidir_init",
                       "org.jacorb.orb.giop.BiDirConnectionInitializer" );
            props.put("ORBInitRef.NameService", "corbaloc::127.0.0.1:8087/NameService");
            s_orb = org.omg.CORBA.ORB.init( args, props );

            ResolveTestService();
            CreateCallbackObject();        

            junit.textui.TestRunner.run (suite());
        } catch (Exception ex) {
            System.out.println("error while running test: " + ex);
        }
    }

    private static void CreateCallbackObject() throws Exception {
        POA rootPoa = (POA) s_orb.resolve_initial_references( "RootPOA" );

        // create orb with bidir policy
        Any any = s_orb.create_any();
        BidirectionalPolicyValueHelper.insert( any, BOTH.value );

        Policy[] policies = new Policy[4];
        policies[0] = 
            rootPoa.create_lifespan_policy(LifespanPolicyValue.TRANSIENT);

        policies[1] = 
            rootPoa.create_id_assignment_policy(IdAssignmentPolicyValue.SYSTEM_ID);

        policies[2] = 
            rootPoa.create_implicit_activation_policy( ImplicitActivationPolicyValue.IMPLICIT_ACTIVATION );

        policies[3] = s_orb.create_policy( BIDIRECTIONAL_POLICY_TYPE.value,
                                           any );

        POA bidirPoa = rootPoa.create_POA( "BiDirPOA",
                                           rootPoa.the_POAManager(),
                                           policies );
        bidirPoa.the_POAManager().activate();
        System.out.println("register callback servants");
        s_cbServant = new ClientCallbackImpl();

        s_cb = CallBackHelper.narrow( 
                                bidirPoa.servant_to_reference( s_cbServant ));

        ClientCallbackIntIncrementerImpl cbIntIncrServant = 
                                             new ClientCallbackIntIncrementerImpl();
        s_cbIntIncr = CallbackIntIncrementerHelper.narrow(
                                                       bidirPoa.servant_to_reference(cbIntIncrServant));
    }

    private static void ResolveTestService() throws Exception {
        org.omg.CosNaming.NameComponent[] serviceName = 
            new org.omg.CosNaming.NameComponent[1];
        serviceName[0] = new NameComponent("test", "");
        org.omg.CosNaming.NamingContext nc = 
            org.omg.CosNaming.NamingContextHelper.narrow(
                s_orb.resolve_initial_references( "NameService" ));        

        org.omg.CORBA.Object service = nc.resolve(serviceName);
        s_testService = TestServiceHelper.narrow(service);
    }

    protected void setUp() throws Exception {
    }

    protected void tearDown() {
    }

    public static Test suite() {
        return new TestSuite(TestClient.class);
    }

    public void testWithoutCallback() throws Exception {
        // without callback
        byte arg1 = 1;
        byte result1 = s_testService.TestIncByte(arg1);
        assertEquals("wrong result 1", arg1 + 1, result1);
    }

    public void testCallbackArgument() throws Exception {
        // for the following callbacks, should use already existing connection for callback (bidir)
        String arg2 = "test";        
        s_testService.string_callback(s_cb, arg2);
        assertEquals("wrong result 2", arg2, s_cbServant.GetMsg());
    }

    public void testCallbackPrereg() throws Exception {
        int arg3 = 3;        
        s_testService.RegisterCallbackIntIncrementer(s_cbIntIncr);
        int result3 = s_testService.IncrementWithCallbackIncrementer(arg3);
        assertEquals("wrong result 3", arg3 + 1, result3);
    }


}
