package Ch.Elca.Iiop.IntegrationTests;

import org.omg.CORBA.ORB;

public class TestServiceImpl extends TestServicePOA {


    public TestServiceImpl(ORB orb) {
    }

    public int EchoLong(int arg) {
        return arg;
    }


}