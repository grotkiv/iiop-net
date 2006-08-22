/* Generated By:JJTree: Do not edit this line. ASTmult_expr.cs */

using System;
using System.Collections;

namespace parser {

public enum MultOps {
  Mult, Div, Mod    
}
    
public class ASTmult_expr : SimpleNode {
        
  #region IFields
    
  private ArrayList m_operations = new ArrayList();
    
  #endregion IFields
        
  public ASTmult_expr(int id) : base(id) {
  }

  public ASTmult_expr(IDLParser p, int id) : base(p, id) {
  }


  /** Accept the visitor. **/
  public override Object jjtAccept(IDLParserVisitor visitor, Object data) {
    return visitor.visit(this, data);
  }
  
  /// <summary>
  /// Adds the mult operation between the child i and i+1.
  /// </summary>
  public void AppendMultOperation(MultOps operation) {
      m_operations.Add(operation);
  }
  
  /// <summary>
  /// Returns the mult operation between child i and i + 1.
  /// </summary>
  public MultOps GetMultOperation(int i) {
      return (MultOps)m_operations[i];
  }  
  
  
  
  
}


}

