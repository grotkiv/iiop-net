/* Generated By:JJTree: Do not edit this line. ASTdeclarator.java */

package parser;

public class ASTdeclarator extends SimpleNode {
  public ASTdeclarator(int id) {
    super(id);
  }

  public ASTdeclarator(IDLParser p, int id) {
    super(p, id);
  }


  /** Accept the visitor. **/
  public Object jjtAccept(IDLParserVisitor visitor, Object data) {
    return visitor.visit(this, data);
  }
}